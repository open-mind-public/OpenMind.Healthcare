---

description: "Task list for the Diet Subdomain feature"
---

# Tasks: Diet Subdomain

**Input**: Design documents from `/specs/001-diet-subdomain/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/diet-api.md](./contracts/diet-api.md)

**Tests**: Included and mandatory. Constitution Principle V requires domain and slice tests for every
new aggregate and command handler, and SC-006 requires a test per rejection rule. Tests are not
optional for this feature.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel — different files, no dependency on another incomplete task
- **[Story]**: US1 (logging, P1), US2 (weight, P2), US3 (planning, P3), US4 (insights, P4)

## Path Conventions

Backend paths are relative to `OpenMind.Healthcare/backend/`, frontend paths to
`OpenMind.Healthcare/frontend/`. Repository-root files are named in full.

---

## Phase 1: Setup

**Purpose**: Bring a compiling, solution-registered service into existence.

- [ ] T001 Create `DietApi/DietApi.csproj` targeting `net10.0` with `Nullable` and `ImplicitUsings`
      enabled, package references matching `QuitSmokingApi.csproj` exactly (MediatR 12.4.1,
      Microsoft.AspNetCore.OpenApi 10.0.0, Scalar.AspNetCore 2.0.0,
      Microsoft.EntityFrameworkCore.Sqlite 10.0.0, Microsoft.EntityFrameworkCore.Design 10.0.0,
      Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0) and a project reference to
      `../Shared/DDD.BuildingBlocks/DDD.BuildingBlocks.csproj`
- [ ] T002 [P] Create `DietApi.Tests/DietApi.Tests.csproj` mirroring `QuitSmokingApi.Tests.csproj`
      (xunit 2.9.3, xunit.runner.visualstudio 3.1.4, Microsoft.NET.Test.Sdk 17.14.1, Shouldly 4.2.1,
      coverlet.collector 6.0.4, global `Using` for Xunit and Shouldly) referencing `DietApi.csproj`
- [ ] T003 Register `DietApi`, `DietApi.Tests` **and the currently-unregistered
      `QuitSmokingApi.Tests`** in `OpenMind.Healthcare.sln` under the `backend` solution folder
      (see plan.md Complexity Tracking — the test gate is vacuous until this is done)
- [ ] T004 [P] Create `DietApi/Properties/launchSettings.json` with an `http` profile on
      `http://localhost:3005` and `ASPNETCORE_ENVIRONMENT=Development`. Do **not** use 3004
- [ ] T005 [P] Create `DietApi/appsettings.json` and `appsettings.Development.json` with
      `ConnectionStrings:DefaultConnection = "Data Source=diet.db"` and a `Jwt` section whose
      `Secret`, `Issuer` and `Audience` match `UserApi` and `QuitSmokingApi` exactly — a mismatch
      here is what breaks SC-011 and FR-036 (one sign-in reaches every service). The separate
      `diet.db` connection string is what satisfies FR-037 (independent store)
- [ ] T006 [P] Create `DietApi/Dockerfile` copying the `QuitSmokingApi/Dockerfile` structure: SDK
      build stage, non-root `appuser`, `/app/data` owned by that user, `EXPOSE 5000`, health check,
      `ENTRYPOINT ["dotnet", "DietApi.dll"]`

**Checkpoint**: `dotnet build OpenMind.Healthcare.sln` succeeds and includes three test projects.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Everything every user story needs. ⚠️ No story work starts until this phase is done.

- [ ] T007 [P] Create `DietApi/Services/UserService.cs` with `IUserService`
      (`GetCurrentUserId`, `GetCurrentUserEmail`) reading `ClaimTypes.NameIdentifier` — copy the
      shape from `QuitSmokingApi/Services/UserService.cs`
- [ ] T008 [P] Create `DietApi/Domain/Enums.cs` (or one file per enum) defining `Sex`,
      `ActivityLevel`, `UnitSystem`, `TargetOrigin`, `GoalDirection`, `MealOccasion`,
      `FoodCategory`, `MeasurementUnit` per [data-model.md](./data-model.md#enumerations)
- [ ] T009 Create `DietApi/Infrastructure/Data/DietDbContext.cs` with the `IMediator`-injecting
      constructor and the `SaveChangesAsync` override that collects, clears and publishes domain
      events — the same implementation as `AppDbContext`. `DbSet`s are added per story
- [ ] T010 Create `DietApi/Program.cs`: `AddOpenApi`, `JsonStringEnumConverter`, MediatR assembly
      registration, `AddDbContext<DietDbContext>` with Sqlite, JWT bearer with
      `ClockSkew = TimeSpan.Zero`, `AddAuthorization`, `AddHttpContextAccessor`, `UserService`
      registrations, the `AllowAngular` CORS policy with the same origins as `QuitSmokingApi`,
      Scalar in Development, and the startup `Database.Migrate()` + initializer block inside a
      logged try/catch
- [ ] T011 [P] Add the `diet-api` service to `OpenMind.Healthcare/docker-compose.yml`: build from
      `DietApi/Dockerfile`, host port **5436**→5000, volume `diet-sqlite-data:/app/data`,
      `ConnectionStrings__DefaultConnection=Data Source=/app/data/diet.db`, on
      `quitsmoking-network`; declare the `diet-sqlite-data` volume; add `diet-api` to the `ui`
      service's `depends_on`
- [ ] T012 [P] Add `/diet-api` to `frontend/proxy.conf.json` targeting `http://localhost:3005` with
      `pathRewrite` `^/diet-api` → `/api`
- [ ] T013 [P] Add a `location /diet-api/` block to `frontend/nginx.conf` proxying to
      `http://diet-api:5000/api/` with the same headers as the existing two blocks.
      **T012 and T013 must both be done** — one without the other passes dev and 404s in Docker
- [ ] T014 [P] Create `DietApi.Tests/TestSupport/SignedInUser.cs` — a fake `IUserService` returning
      a fixed `Guid`, plus a variant returning `null` for the unauthenticated path
- [ ] T015 [P] Create `frontend/src/app/models/diet.models.ts` with the TypeScript interfaces for
      every payload in [contracts/diet-api.md](./contracts/diet-api.md)
- [ ] T016 Create `frontend/src/app/services/diet.service.ts` with `baseUrl = '/diet-api'` and
      empty method stubs; verify `AuthInterceptor` attaches the bearer token to this prefix without
      modification

**Checkpoint**: DietApi starts on 3005, Scalar renders, an authorized probe route returns 401
without a token and 200 with one.

---

## Phase 3: User Story 1 — Log what I eat against a daily target (P1) 🎯 MVP

**Goal**: A person completes diet setup, receives an explained daily target, logs foods from a
seeded catalog or their own, and sees totals, remaining allowance and a per-occasion breakdown.

**Independent Test**: Quickstart V1 — setup, log three foods, verify totals and the three-state day
status, with no weight entry and no plan in existence.

### Domain tests for US1 (write first, watch them fail)

- [ ] T017 [P] [US1] `DietApi.Tests/Domain/EnergyTargetCalculatorTests.cs` — Mifflin-St Jeor for both
      sexes against hand-worked examples, each activity factor, and each goal direction at
      0.5 kg/week (research R1)
- [ ] T018 [P] [US1] `DietApi.Tests/Domain/NutritionTargetTests.cs` — macro percentages convert to
      grams at 4/4/9; a split not summing to 100 throws; `IsBelowSafeFloor` returns true below
      1200 (female) and 1500 (male) and false at exactly the floor
- [ ] T019 [P] [US1] `DietApi.Tests/Domain/NutritionFactsTests.cs` — `Scale` is exact for fractional
      portions; negatives are rejected; rounding matches the 2-dp discipline
- [ ] T020 [P] [US1] `DietApi.Tests/Domain/StartingADietProfileTests.cs` — `Start` derives and
      records a target; implausible height and weight throw; `TargetOn(date)` returns the target in
      force on that date and is unaffected by a later target change (FR-005)
- [ ] T021 [P] [US1] `DietApi.Tests/Domain/SafetyFloorTests.cs` — a below-floor target is **accepted**
      with the flag set, not rejected; acknowledgement is recorded; changing to another below-floor
      target clears the acknowledgement so the warning recurs (FR-004, FR-004a)
- [ ] T022 [P] [US1] `DietApi.Tests/Domain/FoodLoggingTests.cs` — adding scales facts once and stores
      a snapshot; amending recalculates without duplicating; removing clears the contribution;
      totals, remaining (including negative) and per-occasion breakdown are exact (SC-003)
- [ ] T023 [P] [US1] `DietApi.Tests/Domain/LoggedDayBoundaryTests.cs` — future dates and dates before
      `StartedOn` throw; a day with zero entries is distinguishable from an unlogged date and from a
      date outside the tracked period (FR-019)
- [ ] T024 [P] [US1] `DietApi.Tests/Domain/PortionRuleTests.cs` — zero, negative and above-ceiling
      portions each throw the named rule (FR-018, SC-006)

### Domain implementation for US1

- [ ] T025 [P] [US1] `DietApi/Domain/ValueObjects/NutritionFacts.cs` — per-100-unit facts with
      `Create`, `Scale`, `Zero`, `GetEqualityComponents`, private EF constructor
- [ ] T026 [P] [US1] `DietApi/Domain/ValueObjects/Quantity.cs` — amount + `MeasurementUnit`, `Add`
      throwing on unit mismatch
- [ ] T027 [P] [US1] `DietApi/Domain/ValueObjects/BodyMeasurements.cs` — height and starting weight
- [ ] T028 [US1] `DietApi/Domain/ValueObjects/NutritionSnapshot.cs` — food name + already-scaled
      facts (depends on T025)
- [ ] T029 [US1] `DietApi/Domain/ValueObjects/NutritionTarget.cs` — energy, macro split, derived
      grams, `Origin`, `IsBelowSafeFloor(Sex)` (depends on T025)
- [ ] T030 [P] [US1] `DietApi/Domain/Rules/DietProfileRules.cs` — `MacroSplitMustSumTo100Rule`,
      `TargetEnergyMustBePositiveRule`, `HeightMustBePlausibleRule`, `WeightMustBePlausibleRule`
- [ ] T031 [P] [US1] `DietApi/Domain/Rules/FoodLogRules.cs` — `EntryDateCannotBeInFutureRule`,
      `EntryDateCannotPrecedeProfileStartRule`, `PortionMustBePositiveRule`,
      `PortionMustBePlausibleRule`, `NutritionValuesCannotBeNegativeRule`
- [ ] T032 [US1] `DietApi/Domain/Services/EnergyTargetCalculator.cs` — RMR, activity factor, goal
      adjustment, and the plain-language `derivation` sentence required by FR-002
- [ ] T033 [US1] `DietApi/Domain/Entities/TargetRecord.cs` — append-only history entry
- [ ] T034 [US1] `DietApi/Domain/Aggregates/DietProfile.cs` — `Start`, `SetDerivedTarget`,
      `OverrideTarget`, `AcknowledgeBelowFloorTarget`, `TargetOn(date)`, all `asOf`-aware
      (depends on T029-T033)
- [ ] T035 [P] [US1] `DietApi/Domain/Aggregates/Food.cs` — `CreateCatalogItem`, `CreateCustom`,
      `Rename`, `UpdateFacts`, nullable `OwnerUserId`
- [ ] T036 [US1] `DietApi/Domain/Entities/LoggedEntry.cs` — weak nullable `FoodId`, owned `Quantity`
      and `NutritionSnapshot`
- [ ] T037 [US1] `DietApi/Domain/Aggregates/FoodLogDay.cs` — `Open`, `AddEntry`, `AmendEntry`,
      `RemoveEntry`, `GetTotals`, `GetRemaining`, `GetBreakdownByOccasion` (depends on T036)
- [ ] T038 [P] [US1] `DietApi/Domain/Events/` — `DietProfileStartedEvent`, `TargetChangedEvent`,
      `FoodLoggedEvent`
- [ ] T039 [P] [US1] `DietApi/Domain/Repositories/` — `IDietProfileRepository`, `IFoodRepository`,
      `IFoodLogDayRepository`

### Infrastructure for US1

- [ ] T040 [US1] Add `DietProfiles`, `Foods`, `FoodLogDays` to `DietDbContext` with the `OwnsOne` /
      `OwnsMany` configuration, explicit column names, `ValueGeneratedNever` on owned entity ids,
      `Ignore(DomainEvents)`, field-access navigation, and the unique indexes from
      [data-model.md](./data-model.md#persistence-notes). `LoggedEntry.FoodId` gets **no** FK
- [ ] T041 [US1] Generate migration `InitialCreate` into
      `DietApi/Infrastructure/Data/Migrations/`
- [ ] T042 [US1] `DietApi/Infrastructure/Data/DietDbInitializer.cs` — seed ~120 catalog foods across
      the eight categories with per-100-unit facts, guarded by `if (!context.Foods.Any())` with a
      single `SaveChanges()` (FR-013, SC-009)
- [ ] T043 [P] [US1] `DietApi/Infrastructure/Data/Repositories/DietProfileRepository.cs` — including
      the detached-vs-tracked `Update` care that `QuitJourneyRepository` documents
- [ ] T044 [P] [US1] `DietApi/Infrastructure/Data/Repositories/FoodRepository.cs` — search returns
      catalog rows plus the caller's own only (FR-011)
- [ ] T045 [P] [US1] `DietApi/Infrastructure/Data/Repositories/FoodLogDayRepository.cs` —
      `GetByUserAndDateAsync`, `GetRangeAsync`
- [ ] T046 [US1] Register the three repositories and `EnergyTargetCalculator` in `Program.cs`

### Feature slices for US1

- [ ] T047 [P] [US1] `DietApi/Features/Profile/ProfileDtos.cs`
- [ ] T048 [P] [US1] `DietApi/Features/Profile/GetDietProfile/GetDietProfileHandler.cs`
- [ ] T049 [P] [US1] `DietApi/Features/Profile/CreateOrUpdateDietProfile/CreateOrUpdateDietProfileHandler.cs`
      — the setup path behind FR-001
- [ ] T050 [P] [US1] `DietApi/Features/Profile/OverrideTarget/OverrideTargetHandler.cs`
- [ ] T051 [P] [US1] `DietApi/Features/Profile/AcknowledgeBelowFloorTarget/AcknowledgeBelowFloorTargetHandler.cs`
- [ ] T052 [US1] `DietApi/Features/Profile/ProfileEndpoints.cs` — `MapGroup("/api/diet/profile")`,
      `.WithTags("DietProfile")`, `.RequireAuthorization()`, per-route `WithName`/`WithOpenApi`,
      `DomainException` → 400, missing profile → 404
- [ ] T053 [P] [US1] `DietApi/Features/Foods/FoodDtos.cs`
- [ ] T054 [US1] `DietApi/Features/Foods/` — `SearchFoods`, `GetFood`, `CreateCustomFood`,
      `UpdateCustomFood`, `DeleteCustomFood` handlers, one folder each
- [ ] T055 [US1] `DietApi/Features/Foods/FoodsEndpoints.cs` — another person's private food returns
      **404, not 403** (FR-011)
- [ ] T056 [P] [US1] `DietApi/Features/Log/LogDtos.cs` — including the three-state `status` and
      `totals: null` for non-logged days
- [ ] T057 [US1] `DietApi/Features/Log/` — `GetLoggedDay`, `GetLoggedDays`, `AddLogEntry`,
      `AmendLogEntry`, `RemoveLogEntry` handlers. `AddLogEntry` reads the food, scales once, and
      hands the snapshot to the aggregate (research R7)
- [ ] T058 [US1] `DietApi/Features/Log/LogEndpoints.cs`
- [ ] T059 [US1] Call `MapProfileEndpoints()`, `MapFoodsEndpoints()`, `MapLogEndpoints()` in
      `Program.cs`

### Slice tests for US1

- [ ] T060 [P] [US1] `DietApi.Tests/TestSupport/FakeDietProfileRepository.cs`,
      `FakeFoodRepository.cs`, `FakeFoodLogDayRepository.cs`, `DietProfileBuilder.cs`
- [ ] T061 [P] [US1] `DietApi.Tests/Features/ProfileHandlerTests.cs` — setup derives a target;
      unauthenticated throws; override with an invalid split returns the rule's message
- [ ] T062 [P] [US1] `DietApi.Tests/Features/FoodHandlerTests.cs` — search excludes another person's
      custom food; updating someone else's food is not found; deleting a food leaves existing
      entries intact (FR-012)
- [ ] T063 [P] [US1] `DietApi.Tests/Features/LogHandlerTests.cs` — add/amend/remove round trip;
      unauthenticated path; a day outside the tracked period reports `OutsideTrackedPeriod` rather
      than zeros

### Frontend for US1

- [ ] T064 [US1] Add the profile, food and log methods to
      `frontend/src/app/services/diet.service.ts`
- [ ] T065 [P] [US1] `frontend/src/app/components/diet/diet-setup/diet-setup.component.ts` — capture
      DOB, sex, height, weight, activity, unit system and goal; show the derived target with its
      `derivation` sentence; surface the safety-floor warning with an explicit acknowledgement
      control that does **not** block submission; and carry the standing notice that this is a
      self-tracking tool rather than medical or clinical advice (**FR-006**), placed where the
      target is first shown rather than buried in a footer
- [ ] T066 [P] [US1] `frontend/src/app/components/diet/diet-dashboard/diet-dashboard.component.ts` —
      today's totals, remaining, per-occasion breakdown, empty state for an unlogged day
- [ ] T067 [P] [US1] `frontend/src/app/components/diet/food-log/food-log.component.ts` — food
      search, portion entry, occasion selection, amend and remove, custom-food creation
- [ ] T068 [US1] Declare the three components in `frontend/src/app/app.module.ts` and add
      `diet`, `diet/setup`, `diet/log` routes with `canActivate: [AuthGuard]`
- [ ] T069 [US1] Add diet navigation to
      `frontend/src/app/components/navbar/navbar.component.ts`
- [ ] T070 [US1] Present every mass and portion according to the profile's `unitSystem`, converting
      at the presentation edge only (FR-040, research R9)

**Checkpoint**: Quickstart V1 passes end to end. US1 is independently shippable.

---

## Phase 4: User Story 2 — Track my weight toward a goal (P2)

**Goal**: Weigh-ins, trend, distance to goal, and goal achievement.

**Independent Test**: Quickstart V2 — set a goal, record several weigh-ins, verify trend and
distance, with no food entries in existence.

### Tests for US2

- [ ] T071 [P] [US2] `DietApi.Tests/Domain/WeighInTests.cs` — a repeat date amends rather than
      duplicates (FR-021); future dates throw; implausible weights throw (FR-025)
- [ ] T072 [P] [US2] `DietApi.Tests/Domain/WeightProgressTests.cs` — total change, 30-day window
      change and distance to goal across a plateau scenario and a reversal scenario (SC-004); a
      single upward reading inside a downward trend still reports `Improving` (FR-023);
      `NotEnoughData` below the threshold
- [ ] T073 [P] [US2] `DietApi.Tests/Domain/WeightGoalTests.cs` — goal is marked achieved on the
      weigh-in that reaches it; a new goal reopens tracking (FR-024)

### Implementation for US2

- [ ] T074 [P] [US2] `DietApi/Domain/ValueObjects/WeightGoal.cs`
- [ ] T075 [P] [US2] `DietApi/Domain/Entities/WeighIn.cs`
- [ ] T076 [P] [US2] `DietApi/Domain/Rules/WeightRules.cs` — `WeighInCannotBeInFutureRule`,
      `GoalRateMustBePlausibleRule` (`WeightMustBePlausibleRule` already exists from T030)
- [ ] T077 [P] [US2] `DietApi/Domain/ValueObjects/WeightProgress.cs` — the projection returned by
      the aggregate, including the `Improving | Stable | Worsening | NotEnoughData` trend
- [ ] T078 [US2] Extend `DietProfile` with `SetGoal`, `RecordWeighIn`, `RemoveWeighIn`,
      `GetWeightProgress(asOf)` and the owned `WeighIns` collection
- [ ] T079 [P] [US2] `DietApi/Domain/Events/WeightRecordedEvent.cs`, `GoalAchievedEvent.cs`
- [ ] T080 [US2] Add the `WeighIns`, `TargetRecords` and `WeightGoal` mappings to `DietDbContext`
      with the `(ProfileId, Date)` unique index
- [ ] T081 [US2] Generate migration `AddWeightTracking`
- [ ] T082 [P] [US2] `DietApi/Features/Weight/WeightDtos.cs`
- [ ] T083 [US2] `DietApi/Features/Weight/` — `GetWeighIns`, `RecordWeighIn`, `RemoveWeighIn`,
      `GetWeightProgress`, `SetWeightGoal` handlers
- [ ] T084 [US2] `DietApi/Features/Weight/WeightEndpoints.cs`, registered in `Program.cs`
- [ ] T085 [P] [US2] `DietApi.Tests/Features/WeightHandlerTests.cs` — success and unauthenticated
      paths for each command
- [ ] T086 [US2] Add weight methods to `diet.service.ts`
- [ ] T087 [US2] `frontend/src/app/components/diet/weight-tracker/weight-tracker.component.ts` —
      weigh-in entry, trend chart, distance to goal, achieved state; leads with the trend, not the
      latest reading (FR-023); declare and route it in `app.module.ts`

**Checkpoint**: Quickstart V2 passes. US1 and US2 both work independently.

---

## Phase 5: User Story 3 — Plan the week and shop for it (P3)

**Goal**: Recipes, a dated meal plan with per-day projection, a consolidated shopping list, and
one-action confirmation of a planned day into the log.

**Independent Test**: Quickstart V3 — build a plan, verify projection and list consolidation, confirm
a day, without relying on historical logs.

### Tests for US3

- [ ] T088 [P] [US3] `DietApi.Tests/Domain/RecipeTests.cs` — per-serving nutrition is total ÷
      servings; zero servings and an empty ingredient list throw (FR-027)
- [ ] T089 [P] [US3] `DietApi.Tests/Domain/ShoppingListTests.cs` — one ingredient across four meals
      consolidates to a single summed line (SC-005); the same food in `Gram` and `Piece` yields
      **two** lines, never a sum (FR-030)
- [ ] T090 [P] [US3] `DietApi.Tests/Domain/PlanProjectionTests.cs` — per-day totals against target; a
      day far below target carries a warning (FR-028)
- [ ] T091 [P] [US3] `DietApi.Tests/Domain/PlanConfirmationTests.cs` — confirming produces entries
      identical to manual logging (SC-007); a second confirm throws; an unconfirmed day contributes
      nothing to intake (FR-032)

### Implementation for US3

- [ ] T092 [P] [US3] `DietApi/Domain/Entities/RecipeIngredient.cs`
- [ ] T093 [US3] `DietApi/Domain/Aggregates/Recipe.cs` — `PerServing(foods)` takes facts as a
      parameter so the domain performs no data access
- [ ] T094 [P] [US3] `DietApi/Domain/Entities/PlannedMeal.cs` — exactly one of `FoodId`/`RecipeId`
- [ ] T095 [P] [US3] `DietApi/Domain/ValueObjects/ShoppingListLine.cs`
- [ ] T096 [P] [US3] `DietApi/Domain/Rules/PlanningRules.cs` —
      `RecipeMustHaveAtLeastOneIngredientRule`, `RecipeServingsMustBePositiveRule`,
      `PlanRangeMustBeValidRule`, `PlannedMealMustReferenceExactlyOneSourceRule`,
      `DayCannotBeConfirmedTwiceRule`
- [ ] T097 [US3] `DietApi/Domain/Aggregates/MealPlan.cs` — `AddMeal`, `RemoveMeal`, `ProjectDay`,
      `BuildShoppingList`, `MarkDayConfirmed(date, asOf)`
- [ ] T098 [P] [US3] `DietApi/Domain/Events/PlanDayConfirmedEvent.cs`
- [ ] T099 [P] [US3] `DietApi/Domain/Repositories/IRecipeRepository.cs`, `IMealPlanRepository.cs`
- [ ] T100 [US3] Add `Recipes` and `MealPlans` mappings to `DietDbContext`; generate migration
      `AddPlanning`
- [ ] T101 [P] [US3] `DietApi/Infrastructure/Data/Repositories/RecipeRepository.cs`,
      `MealPlanRepository.cs`; register both
- [ ] T102 [P] [US3] `DietApi/Features/Recipes/` — DTOs, CRUD handlers, `RecipesEndpoints.cs`
- [ ] T103 [P] [US3] `DietApi/Features/Plans/PlanDtos.cs`
- [ ] T104 [US3] `DietApi/Features/Plans/` — `CreateMealPlan`, `AddPlannedMeal`, `RemovePlannedMeal`,
      `GetPlanProjection`, `GetShoppingList`, `ConfirmPlannedDay` handlers. `ConfirmPlannedDay`
      writes through `FoodLogDay.AddEntry` so confirmed meals are ordinary entries (FR-031)
- [ ] T105 [US3] `DietApi/Features/Plans/PlansEndpoints.cs`, registered in `Program.cs`
- [ ] T106 [P] [US3] `DietApi.Tests/Features/PlanHandlerTests.cs`,
      `DietApi.Tests/Features/RecipeHandlerTests.cs`
- [ ] T107 [US3] Add recipe and plan methods to `diet.service.ts`
- [ ] T108 [P] [US3] `frontend/src/app/components/diet/meal-planner/meal-planner.component.ts` —
      calendar assignment, per-day projection, below-target warning
- [ ] T109 [P] [US3] `frontend/src/app/components/diet/shopping-list/shopping-list.component.ts` —
      renders separate lines for incompatible units without implying they can be added
- [ ] T110 [US3] Declare and route both components in `app.module.ts`

**Checkpoint**: Quickstart V3 passes. All three primary stories work independently.

---

## Phase 6: User Story 4 — Understand my habits (P4)

**Goal**: Logging consistency, adherence, streaks, and trend over a rolling window.

**Independent Test**: Quickstart V4 — seed a 90-day history with gaps and verify every figure by
hand.

- [ ] T111 [P] [US4] `DietApi.Tests/Domain/DietInsightsTests.cs` — adherence divides by days
      **logged**, not days in period (FR-034); unlogged days are reported as unlogged; current and
      longest on-target streaks across gaps; `NotEnoughData` below the threshold (FR-035, SC-008)
- [ ] T112 [US4] `DietApi/Domain/ValueObjects/DietInsights.cs` and
      `DietApi/Domain/Services/DietInsightsService.cs` — the full FR-033 set (logging consistency,
      adherence, current and longest on-target streaks, windowed average intake), computed over a
      supplied set of days, `asOf`-driven, reusing the trend vocabulary of the existing relapse
      analytics
- [ ] T113 [P] [US4] `DietApi/Features/Insights/InsightsDtos.cs` and
      `GetDietInsights/GetDietInsightsHandler.cs`
- [ ] T114 [US4] `DietApi/Features/Insights/InsightsEndpoints.cs`, registered in `Program.cs`
- [ ] T115 [P] [US4] `DietApi.Tests/Features/InsightsHandlerTests.cs`
- [ ] T116 [US4] Add the insights method to `diet.service.ts`
- [ ] T117 [US4] `frontend/src/app/components/diet/diet-insights/diet-insights.component.ts`;
      declare and route it in `app.module.ts`

**Checkpoint**: Quickstart V4 passes. All four stories complete.

---

## Phase 7: Polish & Cross-Cutting

- [ ] T118 Run the full quickstart V1-V7, including the container pass and the
      delete-`diet.db`-and-restart seeding check (SC-009)
- [ ] T119 Verify SC-010: `dotnet test` for `QuitSmokingApi.Tests` passes unchanged, and the
      quit-smoking features still work with DietApi stopped (FR-038)
- [ ] T120 [P] Update `README.md` — diet endpoints table, the new service in Project Structure, the
      new ports, and a run instruction for `DietApi`
- [ ] T121 [P] Write `OpenMind.Healthcare/adrs/0001-energy-target-formula.md` (research R1),
      `0002-snapshot-logged-nutrition.md` (R7), `0003-diet-aggregate-boundaries.md` (R8) — the
      `adrs/` directory is currently empty
- [ ] T122 [P] Confirm every response enum serialises by name and every diet route requires
      authorization, by reading the generated OpenAPI document at `/openapi/v1.json`
- [ ] T123 Run `/speckit-analyze` for a cross-artifact consistency check before declaring the
      feature done
- [ ] T124 Walk the two experience criteria that no code task can prove: time a first-time person
      from sign-in to seeing their first target (**SC-001**, target under two minutes) and count the
      interactions needed to re-log a previously used food (**SC-002**, three or fewer). Confirm the
      FR-006 notice is visible where the target is presented. Record the results in the PR
      description — if either misses, the fix is a UI task, not a spec change

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (T001-T006)**: no dependencies
- **Foundational (T007-T016)**: needs Setup — **blocks every user story**
- **US1 (T017-T070)**: needs Foundational
- **US2 (T071-T087)**: needs Foundational. Touches `DietProfile` (T078) and `DietDbContext` (T080),
  both introduced by US1 — so in practice it follows US1 rather than running truly parallel
- **US3 (T088-T110)**: needs Foundational; `ConfirmPlannedDay` (T104) needs `FoodLogDay` from US1
- **US4 (T111-T117)**: needs Foundational; genuinely needs US1's logged days to have anything to
  report on
- **Polish (T118-T123)**: needs every story that is being shipped

### Honest note on story independence

The stories are independently *testable and demonstrable*, which is what the spec requires. They are
not fully independently *implementable*: US2 extends the `DietProfile` aggregate that US1 creates,
and US3's confirmation path writes through US1's `FoodLogDay`. Sequential delivery in priority order
is the intended path. Parallelising US2 and US3 across developers would put two people in
`DietProfile.cs` and `DietDbContext.cs` at once — possible with coordination, but not free.

### Within a story

Value objects → rules → entities → aggregates → context mapping → migration → repositories →
handlers → endpoints → slice tests → frontend. Domain tests are written before the domain code they
cover.

### Parallel opportunities

- T004, T005, T006 during Setup
- T007, T008, T011, T012, T013, T014, T015 during Foundational
- All of T017-T024 (US1 domain tests) together
- T025, T026, T027 together; then T030, T031 together
- T043, T044, T045 (repositories) together
- T047-T051, T053, T056 (DTOs and handlers in separate folders) together
- T065, T066, T067 (three separate Angular components) together

---

## Implementation Strategy

**MVP**: Phase 1 → Phase 2 → Phase 3, then stop and validate against quickstart V1. That alone is a
working diet tracker and is deployable.

**Incremental**: add Phase 4, validate V2, deploy. Then Phase 5, validate V3, deploy. Then Phase 6.
Each phase leaves the product releasable.

**Task count**: 124 tasks — 6 setup, 10 foundational, 54 for US1, 17 for US2, 23 for US3, 7 for US4,
7 polish.

## Traceability

Every functional requirement FR-001 to FR-040 and every success criterion SC-001 to SC-011 is cited
by at least one task, data-model entry, or contract section. The check that produced this statement
found four requirements traceable only by implication (FR-001, FR-033, FR-036, FR-037), one with no
coverage at all (FR-006, the non-medical-advice notice), and two success criteria with no validation
step (SC-001, SC-002). All seven are now cited explicitly — in T049, T112, T005, T065 and T124.
