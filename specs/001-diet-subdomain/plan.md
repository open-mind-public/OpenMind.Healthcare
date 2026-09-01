# Implementation Plan: Diet Subdomain

**Branch**: `001-diet-subdomain` | **Date**: 2026-09-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-diet-subdomain/spec.md`

## Summary

Add diet as a fourth bounded context to OpenMind.Healthcare: a new `DietApi` service alongside
`QuitSmokingApi` and `UserApi`, with its own SQLite store, its own migration history, and its own
seeded food catalog, plus the Angular screens that drive it.

The capability covers food logging against a derived daily target (P1), weight tracking toward a
goal (P2), meal planning with a consolidated shopping list (P3), and habit insights (P4). Targets
come from Mifflin-St Jeor with a discrete activity factor and a goal-rate adjustment; food data comes
from a curated catalog shipped with the product rather than an external service; and logged entries
carry a frozen snapshot of the nutrition values in force when they were recorded, so editing a food
never rewrites history.

Nothing in `QuitSmokingApi`, `UserApi`, or `DDD.BuildingBlocks` changes. The two services share only
the JWT configuration, which is what lets one sign-in reach all three.

## Technical Context

**Language/Version**: C# 13 on .NET 10 (verified: SDK 10.0.302); TypeScript 5.6 on Angular 19

**Primary Dependencies**: ASP.NET Core Minimal APIs, MediatR 12.4.1, EF Core 10 (Sqlite + Design),
`Microsoft.AspNetCore.Authentication.JwtBearer` 10, `Microsoft.AspNetCore.OpenApi` 10,
Scalar.AspNetCore 2.0.0, `DDD.BuildingBlocks` (project reference). Frontend adds nothing new —
`HttpClient`, `RouterModule`, and the existing `AuthInterceptor` cover it.

**Storage**: SQLite, own file `diet.db`, own `DietDbContext`, own migration history. No shared
tables, no cross-context foreign keys.

**Testing**: xUnit 2.9.3 + Shouldly 4.2.1 in a new `DietApi.Tests`, mirroring `QuitSmokingApi.Tests`
— hand-written fakes in `TestSupport/`, no mocking framework, no database.

**Target Platform**: Linux containers (`mcr.microsoft.com/dotnet/aspnet:10.0`, non-root `appuser`);
local development on Windows.

**Project Type**: Web application — multi-service backend plus an Angular SPA.

**Performance Goals**: No throughput target. Interactive p95 under 200 ms for a day view; the
`FoodLogDay`-per-day aggregate exists to keep that true as history grows.

**Constraints**: Offline-capable by design — no outbound network calls at runtime. One sign-in must
reach all services. Existing services must keep passing their tests unchanged.

**Scale/Scope**: Personal-scale — a handful of entries per person per day. 4 aggregate roots, ~26
endpoints across 6 route groups, ~120 seeded catalog foods, 6-8 new Angular components.

## Constitution Check

*GATE: evaluated against [constitution.md](../../.specify/memory/constitution.md) v1.0.0.*

### Before Phase 0

| Principle | Assessment |
|---|---|
| **I. Bounded contexts own their data** | PASS — own project, own `DietDbContext`, own `diet.db`, own migrations, own volume. `UserId` from the JWT only; no cross-context FK ([R12](./research.md#r12-cross-service-impact)). |
| **II. Domain model holds the rules** | PASS — 16 `IBusinessRule` classes, Mifflin-St Jeor as a domain calculation, handlers restricted to orchestration ([data-model.md](./data-model.md#business-rules-domainrules)). |
| **III. Vertical slices** | PASS — `Features/<Feature>/<UseCase>/` with `MapGroup` + `RequireAuthorization` per group. |
| **IV. Time is a parameter** | PASS — every date-dependent method takes `DateTime? asOf = null`; `DateOnly` for calendar days. |
| **V. Domain and slice tests** | PASS with a caveat — `DietApi.Tests` is planned. The caveat is that the gate itself is currently broken (see Complexity Tracking). |
| **VI. Migrations and idempotent seeds** | PASS — checked-in migration, `Database.Migrate()` at startup, `DbInitializer` guarded by `.Any()`. |
| **Architecture constraints** | PASS — all 8 new-service requirements addressed; ports allocated in [R4](./research.md#r4-service-topology-and-port-allocation); both proxy files listed in [R5](./research.md#r5-frontend-routing-for-a-third-backend). |

**Gate result: PASS.** One pre-existing defect surfaced, tracked below rather than absorbed silently.

### After Phase 1 design

Re-evaluated against the completed data model and contracts.

| Principle | Assessment |
|---|---|
| **I** | PASS — `LoggedEntry.FoodId` is the only reference that could have tempted a cross-context link; it stays inside DietApi and carries no FK even locally, by design ([R7](./research.md#r7-keeping-logged-history-immune-to-later-edits)). |
| **II** | PASS — shopping-list consolidation, per-serving derivation, and adherence statistics all landed on aggregates, not handlers. `Recipe.PerServing` takes foods as a parameter so the domain stays free of data access. |
| **III** | PASS — 6 route groups, each one endpoints class; every response shape defined in [contracts/diet-api.md](./contracts/diet-api.md). |
| **IV** | PASS — `TargetOn(date)`, `GetWeightProgress(asOf)`, insights windows, and plan projection are all `asOf`-driven. |
| **V** | PASS — SC-006 maps every rejection rule to a test; V1-V7 in [quickstart.md](./quickstart.md) are the manual counterpart. |
| **VI** | PASS — owned-collection mappings, explicit column names, `Ignore(DomainEvents)` on every entity, unique indexes enumerated. |

**Deviation from the reference implementation, stated openly**: `FoodLogDay` is one aggregate *per
person per day*, whereas `QuitJourney` is one *per person*. This breaks the visual symmetry with the
existing service. It is justified in [R8](./research.md#r8-aggregate-boundaries): entry volume grows
without bound, and a per-person aggregate would load a year of meals to add one breakfast. This is a
design choice within Principle II, not a violation of it.

**Gate result: PASS.**

## Project Structure

### Documentation (this feature)

```text
specs/001-diet-subdomain/
├── spec.md              # Feature specification (/speckit-specify + /speckit-clarify)
├── plan.md              # This file (/speckit-plan)
├── research.md          # Phase 0 — 12 resolved decisions
├── data-model.md        # Phase 1 — aggregates, VOs, rules, persistence
├── quickstart.md        # Phase 1 — run + validate, V1-V7
├── contracts/
│   └── diet-api.md      # Phase 1 — HTTP surface
└── tasks.md             # Phase 2 (/speckit-tasks — not created by /speckit-plan)
```

### Source Code (repository root)

```text
OpenMind.Healthcare/backend/
├── DietApi/                                    # NEW SERVICE
│   ├── Domain/
│   │   ├── Aggregates/                         # DietProfile, Food, FoodLogDay, Recipe, MealPlan
│   │   ├── Entities/                           # WeighIn, TargetRecord, LoggedEntry,
│   │   │                                       #   RecipeIngredient, PlannedMeal
│   │   ├── Events/                             # DietProfileStarted, TargetChanged, WeightRecorded,
│   │   │                                       #   GoalAchieved, FoodLogged, PlanDayConfirmed
│   │   ├── Repositories/                       # IDietProfileRepository, IFoodRepository,
│   │   │                                       #   IFoodLogDayRepository, IRecipeRepository,
│   │   │                                       #   IMealPlanRepository
│   │   ├── Rules/                              # 16 IBusinessRule classes
│   │   ├── Services/                           # EnergyTargetCalculator, DietInsightsService
│   │   └── ValueObjects/                       # NutritionFacts, NutritionTarget, Quantity,
│   │                                           #   NutritionSnapshot, WeightGoal,
│   │                                           #   BodyMeasurements, ShoppingListLine
│   ├── Features/
│   │   ├── Profile/                            # GetDietProfile, CreateOrUpdateDietProfile,
│   │   │                                       #   OverrideTarget, AcknowledgeBelowFloorTarget
│   │   ├── Foods/                              # SearchFoods, GetFood, CreateCustomFood,
│   │   │                                       #   UpdateCustomFood, DeleteCustomFood
│   │   ├── Log/                                # GetLoggedDay, GetLoggedDays, AddLogEntry,
│   │   │                                       #   AmendLogEntry, RemoveLogEntry
│   │   ├── Weight/                             # GetWeighIns, RecordWeighIn, RemoveWeighIn,
│   │   │                                       #   GetWeightProgress, SetWeightGoal
│   │   ├── Recipes/                            # CRUD
│   │   ├── Plans/                              # CreateMealPlan, AddPlannedMeal, RemovePlannedMeal,
│   │   │                                       #   GetPlanProjection, GetShoppingList,
│   │   │                                       #   ConfirmPlannedDay
│   │   └── Insights/                           # GetDietInsights
│   ├── Infrastructure/Data/
│   │   ├── DietDbContext.cs
│   │   ├── DietDbInitializer.cs                # ~120 catalog foods, guarded by .Any()
│   │   ├── Migrations/
│   │   └── Repositories/
│   ├── Services/UserService.cs                 # same shape as the other services
│   ├── Properties/launchSettings.json          # port 3005
│   ├── Program.cs
│   ├── Dockerfile
│   └── DietApi.csproj
└── DietApi.Tests/                              # NEW TEST PROJECT
    ├── Domain/                                 # target derivation, safety floor, logging,
    │                                           #   weight trend, shopping list, insights
    ├── Features/                               # handler slice tests
    └── TestSupport/                            # fake repositories, DietProfileBuilder, SignedInUser

OpenMind.Healthcare/frontend/src/app/
├── components/diet/
│   ├── diet-setup/                             # P1 — profile, target, safety-floor warning
│   ├── diet-dashboard/                         # P1 — today's totals, remaining, by occasion
│   ├── food-log/                               # P1 — add/amend/remove entries, food search
│   ├── weight-tracker/                         # P2 — weigh-ins, trend, goal
│   ├── meal-planner/                           # P3 — calendar, projection
│   ├── shopping-list/                          # P3
│   └── diet-insights/                          # P4
├── models/diet.models.ts
└── services/diet.service.ts                    # calls /diet-api/*

Modified:
  OpenMind.Healthcare.sln                       # + DietApi, DietApi.Tests, QuitSmokingApi.Tests
  OpenMind.Healthcare/docker-compose.yml        # + diet-api service, + diet-sqlite-data volume
  OpenMind.Healthcare/frontend/proxy.conf.json  # + /diet-api → localhost:3005
  OpenMind.Healthcare/frontend/nginx.conf       # + /diet-api/ → http://diet-api:5000/api/
  OpenMind.Healthcare/frontend/src/app/app.module.ts   # + declarations, + guarded routes
  .../components/navbar/navbar.component.ts     # + diet navigation
  README.md                                     # + diet endpoints, + structure
```

**Structure Decision**: The existing multi-service layout is extended, not reshaped. `DietApi` and
`DietApi.Tests` sit beside `QuitSmokingApi` and `QuitSmokingApi.Tests` under `backend/`, referencing
the shared `DDD.BuildingBlocks` and nothing else. Frontend diet components are grouped under
`components/diet/` — a slight departure from the flat `components/` layout, chosen because seven
related components would otherwise scatter across an already-flat directory. The Angular app stays
NgModule-based; no migration to standalone components is attempted here.

## Delivery order

The user stories are independently testable, so they ship in priority order and each is demonstrable
on its own.

| Slice | Contents | Demonstrable outcome |
|---|---|---|
| **0. Foundation** | Project, csproj, Program.cs, DietDbContext, JWT, UserService, Dockerfile, compose, both proxy files, solution wiring | DietApi starts, Scalar lists an authorized health route, containers build |
| **1. P1 — Logging** | DietProfile, Food, FoodLogDay, catalog seed, Profile/Foods/Log features, setup + dashboard + log UI | A person sets up, logs meals, sees totals against target |
| **2. P2 — Weight** | WeighIn, WeightGoal, Weight feature, tracker UI | Weigh-ins, trend, distance to goal |
| **3. P3 — Planning** | Recipe, MealPlan, projection, shopping list, confirm-day, planner + list UI | Plan a week, shop from it, confirm a day into the log |
| **4. P4 — Insights** | DietInsightsService, Insights feature, insights UI | Consistency, adherence, streaks, trend |

Slice 0 must complete first. Slices 1-4 are then strictly ordered by priority, and the feature is
releasable after any of them.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| `QuitSmokingApi.Tests` added to the solution as part of this feature | Constitution Principle V and the quality gate `dotnet test` are both vacuous today: the existing test project is on disk but absent from `OpenMind.Healthcare.sln`, so a solution-level build compiles it and a solution-level test run executes nothing. Adding `DietApi.Tests` while leaving that broken would ship a gate that silently passes. | Leaving it out and fixing it separately was considered. Rejected because this feature's own SC-010 ("existing capabilities continue to pass their tests unchanged") is unverifiable until the solution can run those tests. The change is two `.sln` entries and touches no source. |
| JWT configuration duplicated into a third service rather than extracted to `DDD.BuildingBlocks` | Extraction is the right long-term move, but it modifies a shared library that two working services depend on, turning a purely additive feature into one that can regress `QuitSmokingApi` and `UserApi`. | Extracting now was rejected on risk, not merit. Recorded in [R12](./research.md#r12-cross-service-impact) as a follow-up worth its own ADR. |
| `components/diet/` subdirectory breaks the flat `components/` convention | Seven diet components in a flat directory that already holds fourteen would make the diet subdomain invisible in the tree, contradicting the whole point of a bounded context. | A flat layout was rejected for legibility. No code depends on component directory depth. |

## Known follow-ups (not in scope)

1. `QuitSmokingApi`'s `https` launch profile binds `https://localhost:3004`, colliding with
   `UserApi`'s http port. Latent — it only bites when the `https` profile is used. DietApi avoids
   3004 entirely.
2. Extracting shared JWT setup into `DDD.BuildingBlocks` once three services demonstrably need the
   identical block.
3. `OpenMind.Healthcare/adrs/` is empty. R1 (energy formula), R7 (snapshot history), and R8
   (aggregate boundaries) are decisions that outlive this feature and deserve ADRs.
4. The `README.md` still describes the product as "Quit Smoking Tracker". Adding a second subdomain
   makes that name wrong at the top level.
