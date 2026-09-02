# Implementation Plan: Diet Tracking

**Branch**: `001-diet-tracking` | **Date**: 2026-09-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-diet-tracking/spec.md`

## Summary

Add a diet subdomain as a fourth backend service, `DietApi`, alongside `QuitSmokingApi` and
`UserApi`, with its own SQLite database, its own migrations, and its own Angular surface. A member
sets up a plan — goal, start date, body details, activity level — and the service suggests a daily
calorie and macronutrient target they may accept or override. They log meals by picking foods from
a curated, seeded library; each entry snapshots the nutrition values and each day snapshots the
target in force, so neither a library correction nor a later target change can rewrite assessed
history. Days assess themselves as on target, over target, or not logged; a calendar, streaks,
weight trend, achievements, and curated guidance read off that.

Three decisions shape the construction. A logged day is its own aggregate rather than a collection
owned by the plan, because food entries accumulate three to six per day and loading a member's
whole history to add one breakfast item does not scale (R-004). Calories are stored as integers and
each day persists its own totals, because EF Core maps `decimal` to SQLite `TEXT` and cannot
average it, and because reading one row per day is what keeps a three-year calendar under a second
(R-010). Earned achievements are persisted rather than derived, because the spec forbids revoking
one and a derived design cannot honour that (R-007).

## Technical Context

**Language/Version**: C# 13 / .NET 10; TypeScript 5.x / Angular 19

**Primary Dependencies**: ASP.NET Core Minimal APIs, MediatR 12.4.1, EF Core 10 (Sqlite +
Design), Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0, Microsoft.AspNetCore.OpenApi
10.0.0, Scalar.AspNetCore 2.0.0, `Shared/DDD.BuildingBlocks` (project reference)

**Storage**: SQLite, own database file `diet.db`, own migration history, own Docker volume. No
shared context, no cross-context foreign key.

**Testing**: xUnit 2.9.3 with Shouldly 4.2.1, in-memory fakes from `TestSupport/`, no mocking
framework, no database in tests. New project `DietApi.Tests`, registered in the solution (R-014).

**Target Platform**: Linux container (`mcr.microsoft.com/dotnet/aspnet:10.0`), non-root `appuser`,
port 5000, `/app/data` volume; browser front end served by nginx.

**Project Type**: Web application — multi-service .NET backend plus an Angular single-page front
end, orchestrated by Docker Compose.

**Performance Goals**: Calendar, weight trend, and statistics under 1 second for a member with 3
years of daily history (SC-006). Food search under 1 second (SC-004). Day totals update without a
manual refresh (SC-005).

**Constraints**: Calories as `int`, macro grams as `decimal` never aggregated in SQL (R-010). All
instants UTC; calendar days as `DateOnly`. Every time-dependent domain method takes
`DateTime? asOf = null`. Metric storage, display units client-side (R-012).

**Scale/Scope**: 6 user stories, 44 functional requirements, 13 success criteria. Roughly 7 feature
slices, 3 member-owned aggregates plus 3 reference aggregates, ~20 endpoints, a 150-200 item seeded
food library, and 7 new Angular components.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution v1.0.0, ratified 2026-09-01.

### Initial check (before Phase 0)

| Principle | Verdict | How this plan satisfies it |
|--------|--------|--------|
| I. Bounded contexts own their data | PASS | `DietApi` is a separate project with its own `DietDbContext`, its own `diet.db`, its own migrations, and its own compose volume. It reads no other context's tables. `UserId` is the only identifier crossing the boundary, read from the JWT via `IUserService.GetCurrentUserId()` and never accepted from a request (FR-043). Body details needed for target suggestions are held locally rather than fetched from `UserApi`. |
| II. The domain model holds the rules | PASS | Every rule lands in `Domain/` — aggregates with private setters and intention-revealing methods, `IBusinessRule` implementations in `Domain/Rules/`, immutable value objects with static factories. Handlers resolve the user, load one aggregate, call one method, persist, map. Target suggestion, day assessment, and streak calculation are domain services, not handler logic. |
| III. Vertical slices, not layers | PASS | `Features/<Feature>/<UseCase>/<UseCase>Handler.cs`, one command or query record and its handler per file, one static `<Feature>Endpoints.cs` per group with `MapGroup("/api/<kebab-case>")`, `.WithTags`, `.RequireAuthorization()`, and per-route `.WithName().WithOpenApi()`. |
| IV. Time is a parameter | PASS | `asOf` flows through every date-sensitive method: day assessment, streak calculation, plan start validation, entry date validation, weight trend. Stored instants UTC, calendar concepts `DateOnly`. |
| V. Domain and slice tests ship with the feature | PASS | `DietApi.Tests` mirrors source layout, registered in the solution along with the pre-existing `QuitSmokingApi.Tests` (R-014). Every aggregate gets domain tests proving each `IBusinessRule` throws and covering boundary values; every command handler gets a success-path and an unauthenticated-path slice test. |
| VI. Migrations and idempotent seeds | PASS | Checked-in EF Core migrations; `Database.Migrate()` in a logged try/catch at startup followed by `DbInitializer`; every seed guarded by `if (!context.X.Any())` with a single `SaveChanges()`; `OwnsOne`/`OwnsMany` with explicit column names; `Ignore(e => e.DomainEvents)` on every mapped entity. |

**Architecture & Technology Constraints**: all eight items are addressed — project referencing
`DDD.BuildingBlocks` and registered in the solution; identical JWT parameters including
`ClockSkew = TimeSpan.Zero`; own `IUserService` plus `AddHttpContextAccessor()`;
`JsonStringEnumConverter`; the same CORS origin list; a multi-stage Dockerfile with non-root
`appuser`, `/app/data`, a health check, port 5000; a compose service with its own volume and host
port; and a non-colliding dev port. The full allocation table is R-011.

**Frontend routing**: `/diet-api` is added to both `frontend/proxy.conf.json` and
`frontend/nginx.conf`, per the constitution's warning that missing either makes it work in dev and
break in Docker.

**Result**: PASS, no violations. Proceed to Phase 0.

### Post-design re-check (after Phase 1)

Re-evaluated against [data-model.md](./data-model.md) and [contracts/](./contracts/).

| Principle | Verdict | Note |
|--------|--------|--------|
| I | PASS | No design artifact introduces a cross-context read or foreign key. |
| II | PASS | Data model places all 11 business rules inside aggregates; the contract's error responses are the domain exceptions surfacing, not handler-computed results. |
| III | PASS | Contract groups map one-to-one onto feature folders in the source tree below. |
| IV | PASS | Every derived value in the data model — day state, streaks, trend, achievement eligibility — takes `asOf`. |
| V | PASS | Test layout enumerated in the source tree; quickstart runs it. |
| VI | PASS | Seed set and idempotency guards specified in the data model. |

**One deviation to record**: the aggregate split in R-004 departs from the `QuitJourney`
precedent. It violates no principle — Principle II asks that rules live in the domain, not that a
context have one aggregate — but it is a conscious break from the reference implementation, so it
is logged in Complexity Tracking below.

**Result**: PASS. Ready for `/speckit-tasks`.

## Project Structure

### Documentation (this feature)

```text
specs/001-diet-tracking/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── README.md
│   └── rest-api.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created here)
```

### Source Code (repository root)

```text
OpenMind.Healthcare/backend/DietApi/
├── DietApi.csproj                       # net10.0, refs DDD.BuildingBlocks
├── Program.cs                           # JWT, CORS, MediatR, DI, migrate+seed, /health
├── Dockerfile                           # multi-stage, appuser, /app/data, port 5000
├── appsettings.json                     # Jwt secret/issuer/audience identical to peers
├── Properties/launchSettings.json       # http://localhost:3005 only
├── Domain/
│   ├── Aggregates/
│   │   ├── DietPlan.cs                  # owns WeightReadings, UnlockedAchievements
│   │   ├── LoggedDay.cs                 # owns FoodEntries, holds target + total snapshots
│   │   ├── FoodLibraryItem.cs           # owns ServingSizes (seeded)
│   │   ├── DietAchievement.cs           # seeded definition
│   │   └── EatingTip.cs                 # seeded
│   ├── Entities/
│   │   ├── FoodEntry.cs
│   │   ├── WeightReading.cs
│   │   ├── ServingSize.cs
│   │   └── UnlockedAchievement.cs
│   ├── ValueObjects/
│   │   ├── BodyMetrics.cs               # height, age, sex
│   │   ├── ActivityLevel.cs             # enum
│   │   ├── GoalType.cs                  # enum
│   │   ├── MealType.cs                  # enum
│   │   ├── NutritionValues.cs           # int calories + decimal macro grams
│   │   ├── NutritionTargets.cs
│   │   ├── TargetSource.cs              # enum: Suggested | MemberSet
│   │   ├── DayState.cs                  # enum: NotLogged | OnTarget | OverTarget
│   │   ├── DayAssessment.cs
│   │   ├── DietStatistics.cs
│   │   ├── WeightTrend.cs
│   │   └── TargetSuggestion.cs
│   ├── Rules/
│   │   ├── DietPlanRules.cs
│   │   ├── FoodEntryRules.cs
│   │   └── WeightReadingRules.cs
│   ├── Services/
│   │   ├── TargetSuggestionService.cs   # Mifflin-St Jeor + activity + goal + floor
│   │   ├── StreakCalculator.cs          # over ordered day states, takes asOf
│   │   └── DietAchievementStatusService.cs
│   └── Repositories/
│       ├── IDietPlanRepository.cs
│       ├── ILoggedDayRepository.cs
│       ├── IFoodLibraryRepository.cs
│       ├── IDietAchievementRepository.cs
│       └── IEatingTipRepository.cs
├── Features/
│   ├── DietPlan/                        # /api/diet-plan
│   │   ├── DietPlanEndpoints.cs
│   │   ├── DietPlanDtos.cs
│   │   ├── GetDietPlan/
│   │   ├── CreateDietPlan/
│   │   ├── UpdateDietPlan/
│   │   └── SuggestTargets/
│   ├── FoodLog/                         # /api/food-log
│   │   ├── FoodLogEndpoints.cs
│   │   ├── FoodLogDtos.cs
│   │   ├── GetDay/
│   │   ├── GetDayRange/
│   │   ├── AddFoodEntry/
│   │   ├── UpdateFoodEntry/
│   │   └── DeleteFoodEntry/
│   ├── Weight/                          # /api/weight
│   │   ├── WeightEndpoints.cs
│   │   ├── WeightDtos.cs
│   │   ├── GetWeightTrend/
│   │   ├── RecordWeight/
│   │   └── DeleteWeightReading/
│   ├── FoodLibrary/                     # /api/food-library
│   │   ├── FoodLibraryEndpoints.cs
│   │   ├── FoodLibraryDtos.cs
│   │   ├── SearchFoods/
│   │   └── GetFood/
│   ├── DietStats/                       # /api/diet-stats
│   │   ├── DietStatsEndpoints.cs
│   │   ├── DietStatsDtos.cs
│   │   └── GetDietStats/
│   ├── DietAchievements/                # /api/diet-achievements
│   │   ├── DietAchievementsEndpoints.cs
│   │   ├── DietAchievementDtos.cs
│   │   ├── GetAllDietAchievements/
│   │   ├── GetUnlockedDietAchievements/
│   │   └── CheckNewDietAchievements/
│   └── DietGuidance/                    # /api/diet-guidance
│       ├── DietGuidanceEndpoints.cs
│       ├── DietGuidanceDtos.cs
│       ├── GetEatingTips/
│       └── GetDailyEncouragement/
├── Infrastructure/Data/
│   ├── DietDbContext.cs                 # domain-event dispatch on SaveChangesAsync
│   ├── DbInitializer.cs                 # guarded seeds, one SaveChanges
│   ├── Migrations/
│   └── Repositories/
└── Services/
    └── UserService.cs                   # IUserService, claims-based

OpenMind.Healthcare/backend/DietApi.Tests/
├── DietApi.Tests.csproj                 # xunit + Shouldly, refs DietApi
├── Domain/                              # aggregate behaviour, rules, calculations
├── Features/                            # handler slice tests
└── TestSupport/
    ├── FakeDietPlanRepository.cs
    ├── FakeLoggedDayRepository.cs
    ├── FakeFoodLibraryRepository.cs
    ├── DietPlanBuilder.cs
    ├── LoggedDayBuilder.cs
    └── SignedInUser.cs

OpenMind.Healthcare/frontend/src/app/
├── components/
│   ├── diet-setup/                      # US1
│   ├── diet-dashboard/                  # US2 - today's log
│   ├── food-search/                      # US2 - library picker
│   ├── diet-calendar/                   # US3
│   ├── weight-tracker/                  # US4
│   ├── diet-achievements/               # US5
│   └── diet-guidance/                   # US6
├── services/diet.service.ts
└── models/diet.models.ts

# Modified, not new
OpenMind.Healthcare.sln                          # + DietApi, DietApi.Tests, QuitSmokingApi.Tests
OpenMind.Healthcare/docker-compose.yml           # + diet-api service, + diet-sqlite-data volume
OpenMind.Healthcare/frontend/proxy.conf.json     # + /diet-api -> localhost:3005, rewrite to /api
OpenMind.Healthcare/frontend/nginx.conf          # + /diet-api/ -> http://diet-api:5000/api/
OpenMind.Healthcare/frontend/src/app/app.module.ts  # + 7 declarations, + 7 guarded routes
```

**Structure Decision**: Web application with a service-per-subdomain backend. `DietApi` is a
sibling of `QuitSmokingApi` and `UserApi` under `OpenMind.Healthcare/backend/`, sharing only
`Shared/DDD.BuildingBlocks` by project reference. Inside it, the layout is the vertical-slice shape
Principle III mandates and `QuitSmokingApi` already demonstrates: `Domain/` for rules, `Features/`
by use case, `Infrastructure/Data/` for persistence, `Services/` for the claims reader. The front
end extends the existing NgModule-based Angular app rather than introducing a second one, because
members reach diet through the same shell and the same sign-in.

## Complexity Tracking

> Recorded per the constitution's compliance-review clause: a deliberate departure from the
> reference implementation, stated with what was tried instead and why it was rejected.

| Deviation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| `LoggedDay` is its own aggregate rather than an owned collection of `DietPlan`, unlike `SmokedDay` on `QuitJourney` | Food entries accrue 3-6 per day, so three years is ~5,000 owned rows. Mirroring `QuitJourney` would load, track and re-save that entire history to add one breakfast item, putting SC-006 (under 1s) out of reach and making every write scale with tenure. No invariant spans two days: FR-031/032/033 are per-day, FR-001-011 are per-plan, and streaks only project. | Owning everything on `DietPlan` was the first design and is the more consistent one. It was rejected on the growth argument alone; at `SmokedDay` volumes it would have been correct. Making `FoodEntry` its own aggregate was also rejected — it pushes the per-day total invariant out of the domain and into a handler, breaking Principle II. |
| Each `LoggedDay` persists its own calorie and macro totals, duplicating what its entries already say | FR-035 averages daily intake across days, and EF Core maps `decimal` to SQLite `TEXT`, which cannot be averaged numerically. Storing `int` kcal per day makes the average exact, and reading one row per day instead of every entry is what keeps a three-year calendar under a second. | Computing totals from entries on every read carries no denormalisation, but every calendar render would load every entry in the period and fail SC-006. The invariant is safe here only because R-004 keeps entries and total inside one aggregate, recomputed together; a domain test asserts it directly. |
| Earned achievements are persisted, where the smoking area derives status via `AchievementStatusService` | FR-039 forbids revoking an earned achievement, and FR-038 requires the date it was earned. A derived design supplies neither: deleting a mis-logged entry would make a badge vanish. | Mirroring the smoking area exactly was preferred for consistency and rejected because it fails FR-039 and User Story 5 scenario 4 outright. |
