---

description: "Task list for Diet Tracking implementation"
---

# Tasks: Diet Tracking

**Input**: Design documents from `/specs/001-diet-tracking/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Test tasks are **required**, not optional. Constitution Principle V states that every new
aggregate ships with domain tests proving each `IBusinessRule` throws and covering calculations at
boundary values, and every new command handler ships with a slice test for the success path and the
unauthenticated path. Test tasks below are written to that standard.

**Organization**: Tasks are grouped by user story. Each story is a deployable increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1-US6)
- Paths are repository-relative from `c:\Users\tung.le\Data\git-personal\OpenMind.Healthcare`

## Path Conventions

Web application, service per subdomain:

- Backend service: `OpenMind.Healthcare/backend/DietApi/`
- Backend tests: `OpenMind.Healthcare/backend/DietApi.Tests/`
- Front end: `OpenMind.Healthcare/frontend/src/app/`
- Solution: `OpenMind.Healthcare.sln` (repository root, outside the inner app folder)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the service, register it, and claim its ports, volume, and proxy routes.

Port and volume allocation is fixed by [research.md](./research.md) R-011. Do not improvise values —
3005 dev, 5436 Docker host, `diet-sqlite-data`, `/diet-api`.

- [ ] T001 Create the DietApi project file at `OpenMind.Healthcare/backend/DietApi/DietApi.csproj` targeting `net10.0` with `Nullable`/`ImplicitUsings` enabled, package references matching `QuitSmokingApi.csproj` (MediatR 12.4.1, Microsoft.AspNetCore.OpenApi 10.0.0, Scalar.AspNetCore 2.0.0, Microsoft.EntityFrameworkCore.Sqlite 10.0.0, Microsoft.EntityFrameworkCore.Design 10.0.0, Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0), and a project reference to `../Shared/DDD.BuildingBlocks/DDD.BuildingBlocks.csproj`
- [ ] T002 Create the test project file at `OpenMind.Healthcare/backend/DietApi.Tests/DietApi.Tests.csproj` mirroring `QuitSmokingApi.Tests.csproj` (xunit 2.9.3, Shouldly 4.2.1, Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4, global `Using` entries for Xunit and Shouldly) with a project reference to `../DietApi/DietApi.csproj`
- [ ] T003 Register three projects in `OpenMind.Healthcare.sln`: the new `DietApi` and `DietApi.Tests`, plus the pre-existing but unregistered `QuitSmokingApi.Tests` — without the last one `dotnet test` on the solution silently runs nothing, which is the constitution's outstanding `TODO(TEST_PROJECT_IN_SOLUTION)` (research.md R-014)
- [ ] T004 [P] Create `OpenMind.Healthcare/backend/DietApi/appsettings.json` with connection string `Data Source=diet.db` and a `Jwt` section whose `Secret`, `Issuer`, and `Audience` are byte-identical to `QuitSmokingApi/appsettings.json`, so one sign-in works across services
- [ ] T005 [P] Create `OpenMind.Healthcare/backend/DietApi/Properties/launchSettings.json` with an **http profile only** on `http://localhost:3005`. Deliberately do not copy QuitSmokingApi's https profile, which binds 3004 and collides with UserApi (research.md R-011)
- [ ] T006 [P] Create `OpenMind.Healthcare/backend/DietApi/Dockerfile` following the QuitSmokingApi pattern: SDK 10.0 build stage copying `Shared/DDD.BuildingBlocks` first, aspnet 10.0 runtime stage, non-root `appuser`, `/app/data` owned by that user, `EXPOSE 5000`, and a `HEALTHCHECK` against `/health`
- [ ] T007 [P] Add a `diet-api` service to `OpenMind.Healthcare/docker-compose.yml` on host port `5436` mapped to container `5000`, with connection string `Data Source=/app/data/diet.db`, a new named volume `diet-sqlite-data` mounted at `/app/data`, and membership of `quitsmoking-network`; add `diet-api` to the `ui` service's `depends_on`
- [ ] T008 [P] Add a `/diet-api` entry to `OpenMind.Healthcare/frontend/proxy.conf.json` targeting `http://localhost:3005` with `pathRewrite` of `^/diet-api` to `/api`, matching the existing `/user-api` entry
- [ ] T009 [P] Add a `location /diet-api/` block to `OpenMind.Healthcare/frontend/nginx.conf` proxying to `http://diet-api:5000/api/` with the same headers as the `/user-api/` block. Omitting this while doing T008 makes the feature work in dev and break in Docker — the exact failure the constitution warns about

**Checkpoint**: The project exists, builds empty, and every shared resource it needs is claimed.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infrastructure every user story depends on — host wiring, persistence plumbing, the two
nutrition value objects, and the shared enums.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T010 [P] Create the shared enums in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/`: `GoalType` (LoseWeight, Maintain, GainWeight, EatConsistently), `ActivityLevel` (Sedentary, LightlyActive, ModeratelyActive, VeryActive, ExtraActive), `MealType` (Breakfast, Lunch, Dinner, Snack), `BiologicalSex` (Female, Male), `TargetSource` (Suggested, MemberSet), `DayState` (NotLogged, OnTarget, OverTarget), `FoodCategory`, `TipCategory`, and `AchievementCriterion` — one file per enum
- [ ] T011 [P] Create `NutritionValues` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/NutritionValues.cs` deriving from `ValueObject`, with `int Calories` (whole kcal — research.md R-010), `decimal ProteinG/CarbsG/FatG`, static `Create` and `Zero` factories, `Plus(NutritionValues)` and `Times(decimal quantity)` methods, and `GetEqualityComponents()` over all four fields
- [ ] T012 [P] Create `NutritionTargets` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/NutritionTargets.cs` with `int Calories` and nullable `decimal? ProteinG/CarbsG/FatG` (macros are optional per spec Assumptions), a static `Create` factory checking `DailyCalorieTargetMustBePositiveRule`, and `GetEqualityComponents()`
- [ ] T013 Create `OpenMind.Healthcare/backend/DietApi/Services/UserService.cs` with `IUserService` exposing `GetCurrentUserId()` and `GetCurrentUserEmail()`, reading `ClaimTypes.NameIdentifier` and `ClaimTypes.Email` via `IHttpContextAccessor` — a copy of the QuitSmokingApi implementation, which is the only sanctioned source of member identity (FR-043)
- [ ] T014 Create `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DietDbContext.cs` with the primary-constructor `(DbContextOptions<DietDbContext> options, IMediator mediator)` shape and the `SaveChangesAsync` override that collects domain events from tracked entities, clears them, saves, then publishes each through MediatR. Leave `DbSet` properties and `OnModelCreating` configuration empty — each story adds its own
- [ ] T015 Create `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DbInitializer.cs` with a static `Initialize(DietDbContext)` using the `hasChanges` flag pattern: each seed guarded by `if (!context.X.Any())`, with exactly one `SaveChanges()` at the end (Principle VI). Seed bodies are added per story
- [ ] T016 Create `OpenMind.Healthcare/backend/DietApi/Program.cs` wiring, in the order QuitSmokingApi uses: `AddOpenApi`, `ConfigureHttpJsonOptions` with `JsonStringEnumConverter`, `AddMediatR` from the assembly, `AddDbContext<DietDbContext>` with SQLite, JWT bearer authentication with `ValidateIssuerSigningKey/Issuer/Audience/Lifetime` and `ClockSkew = TimeSpan.Zero`, `AddAuthorization`, `AddHttpContextAccessor`, `UserService`/`IUserService` scoped registrations, the `AllowAngular` CORS policy with the same origin list, Scalar in development, `UseCors/UseAuthentication/UseAuthorization`, an unauthenticated `GET /health` returning 200 (research.md R-013), and the startup scope that calls `context.Database.Migrate()` inside a logged try/catch followed by `DbInitializer.Initialize`
- [ ] T017 [P] Create `OpenMind.Healthcare/backend/DietApi.Tests/TestSupport/SignedInUser.cs` building the real `UserService` over a `DefaultHttpContext` carrying identity claims, with `WithId(Guid, string email)` and `Anonymous()` factories — ported from the QuitSmokingApi test support so handlers see production behaviour
- [ ] T018 [P] Create `OpenMind.Healthcare/frontend/src/app/models/diet.models.ts` with TypeScript interfaces for every response shape in [contracts/rest-api.md](./contracts/rest-api.md), using string unions for the enums so they match the name-based wire format
- [ ] T019 [P] Create `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts` as an injectable service with the `/diet-api` base path and no methods yet — each story adds its own, and the auth interceptor already attaches the bearer token

**Checkpoint**: Foundation ready. The service starts, answers `/health`, applies migrations, and
rejects unauthenticated calls. User story implementation can begin.

---

## Phase 3: User Story 1 - Set Up a Personal Diet Plan (Priority: P1) 🎯 MVP

**Goal**: A member enters their goal, start date, body details, and activity level; the service
suggests a daily calorie and macro target they can accept or override; the plan persists.

**Independent Test**: Sign in, complete setup, reload — the plan persists with the right goal, start
date, and targets. Override the suggestion and confirm `targetSource` flips to `MemberSet`. Set a
target below the safe floor and confirm it saves **with a warning** rather than being blocked.

**Note on scope**: `WeightReading` is built here, not in US4. `DietPlan.Create` stores the setup
weight as the first reading so current weight has one source of truth (FR-017, data-model.md). US4
adds the trend, the endpoints, and the UI on top of it.

### Domain for User Story 1

- [ ] T020 [P] [US1] Create `BodyMetrics` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/BodyMetrics.cs` with `HeightCm`, `Age`, `Sex`, and a static `Create` factory. Weight is deliberately absent — it lives in `WeightReadings` (data-model.md)
- [ ] T021 [P] [US1] Create `TargetSuggestion` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/TargetSuggestion.cs` exposing `SuggestedTargets`, `RestingEnergyKcal`, `ActivityAdjustedKcal`, `GoalAdjustmentKcal`, `WasClampedToFloor`, and `FloorKcal`, so the UI can explain the number rather than assert it (FR-010)
- [ ] T022 [P] [US1] Create `WeightReading` entity in `OpenMind.Healthcare/backend/DietApi/Domain/Entities/WeightReading.cs` with `DietPlanId`, `DateOnly Date`, `decimal WeightKg`, `RecordedAt`, private setters, a private parameterless constructor, and a static factory
- [ ] T023 [US1] Create `OpenMind.Healthcare/backend/DietApi/Domain/Rules/DietPlanRules.cs` implementing `IBusinessRule` for `PlanStartDateCannotBeInFutureRule`, `DailyCalorieTargetMustBePositiveRule`, `HeightMustBePlausibleRule` (50-250 cm), `AgeMustBePlausibleRule` (13-120), and `TargetWeightMustBePlausibleRule` (20-500 kg), each naming itself via `nameof`
- [ ] T024 [US1] Create `OpenMind.Healthcare/backend/DietApi/Domain/Rules/WeightReadingRules.cs` with `WeightDateCannotBeInFutureRule` and `WeightMustBePlausibleRule` (20-500 kg)
- [ ] T025 [US1] Create the `DietPlan` aggregate in `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/DietPlan.cs` deriving from `AggregateRoot`, with private setters, a private parameterless constructor for EF, a private backing field for weight readings exposed as `IReadOnlyCollection<WeightReading>`, and methods `Create`, `UpdatePlan`, `SetTargets`, `RecordWeight` (replacing any reading on the same date), `RemoveWeightReading`, and `CurrentWeightKg(asOf)`. Every date-sensitive method takes `DateTime? asOf = null` defaulting to `DateTime.UtcNow` (Principle IV). Enforce invariants with `CheckRule`, call `SetUpdated()`, and `Emit` domain events
- [ ] T026 [P] [US1] Create the domain events `DietPlanCreatedEvent` and `TargetsChangedEvent` in `OpenMind.Healthcare/backend/DietApi/Domain/Events/`
- [ ] T027 [US1] Create `TargetSuggestionService` in `OpenMind.Healthcare/backend/DietApi/Domain/Services/TargetSuggestionService.cs` implementing Mifflin-St Jeor resting energy, the five activity multipliers, the four goal adjustments, the safe-floor clamp, and the goal-dependent macro split — all values exactly as fixed in research.md R-001, R-002, and R-003. Hold the floor figures (1,200 female / 1,500 male) as named constants in one place, since they are the numbers most likely to be revised
- [ ] T028 [US1] Create `IDietPlanRepository` in `OpenMind.Healthcare/backend/DietApi/Domain/Repositories/IDietPlanRepository.cs` with `GetByUserIdAsync`, `AddAsync`, and `UpdateAsync`, and its implementation in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Repositories/DietPlanRepository.cs`. Copy the detached-entity guard from `QuitJourneyRepository.UpdateAsync` — calling `Update()` on a tracked aggregate marks new child rows Modified instead of Added
- [ ] T029 [US1] Configure `DietPlan` in `DietDbContext.OnModelCreating` at `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DietDbContext.cs`: unique index on `UserId`, `OwnsOne` for `BodyMetrics` and `Targets` with explicit column names, `OwnsMany` for `WeightReadings` with `ToTable`, `WithOwner().HasForeignKey`, `HasKey`, `ValueGeneratedNever()`, a unique index on `(DietPlanId, Date)`, and `Ignore(d => d.DomainEvents)`; set `UsePropertyAccessMode(PropertyAccessMode.Field)` on the navigation; store enums via `HasConversion<string>().HasMaxLength(50)`; add the `DietPlans` DbSet
- [ ] T030 [US1] Generate the initial migration into `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Migrations/` with `dotnet ef migrations add InitialCreate -o Infrastructure/Data/Migrations`, and verify it produces a working schema against an empty database

### Tests for User Story 1

- [ ] T031 [P] [US1] Create `OpenMind.Healthcare/backend/DietApi.Tests/TestSupport/FakeDietPlanRepository.cs` as an in-memory `IDietPlanRepository` keyed by user id, exposing a `SaveCount` so tests can prove a command persisted, with `Empty()` and `Containing(plan)` factories
- [ ] T032 [P] [US1] Create `OpenMind.Healthcare/backend/DietApi.Tests/TestSupport/DietPlanBuilder.cs` building a `DietPlan` around a single pinned `Clock` that tests pass back as `asOf`, with fluent methods for goal, start date, body metrics, activity level, targets, and seeded weight readings
- [ ] T033 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/DietPlanRulesTests.cs` proving each of the seven rules from T023 and T024 throws when broken and passes at its boundary values
- [ ] T034 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/TargetSuggestionTests.cs` covering both sexes, all five activity levels, all four goals, and the floor clamp — asserting that a suggestion is never returned below the floor and that `WasClampedToFloor` reports honestly
- [ ] T035 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/WeightRecordingTests.cs` proving a second reading on the same date replaces the first rather than adding a second, and that `CurrentWeightKg` returns the most recent reading at or before `asOf`
- [ ] T036 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/TargetChangeTests.cs` proving `UpdatePlan` leaves `Targets` untouched and only `SetTargets` changes them, with `TargetSource` recorded correctly for each path (FR-006, FR-009)

### Endpoints for User Story 1

- [ ] T037 [P] [US1] Create `OpenMind.Healthcare/backend/DietApi/Features/DietPlan/DietPlanDtos.cs` with the request and response records for all five plan routes in [contracts/rest-api.md](./contracts/rest-api.md)
- [ ] T038 [P] [US1] Create `SuggestTargetsHandler` in `OpenMind.Healthcare/backend/DietApi/Features/DietPlan/SuggestTargets/SuggestTargetsHandler.cs` — a query record plus handler in one file, calling `TargetSuggestionService` and persisting nothing
- [ ] T039 [US1] Create `CreateDietPlanHandler` in `OpenMind.Healthcare/backend/DietApi/Features/DietPlan/CreateDietPlan/CreateDietPlanHandler.cs`, resolving the user, rejecting a second plan, storing the supplied current weight as the first `WeightReading`, and returning a below-floor warning alongside a **successful** create when the member overrode under the floor (FR-008)
- [ ] T040 [P] [US1] Create `GetDietPlanHandler` in `OpenMind.Healthcare/backend/DietApi/Features/DietPlan/GetDietPlan/GetDietPlanHandler.cs` returning null when the member has no plan, so the endpoint can answer 404
- [ ] T041 [US1] Create `UpdateDietPlanHandler` in `OpenMind.Healthcare/backend/DietApi/Features/DietPlan/UpdateDietPlan/UpdateDietPlanHandler.cs` returning the updated plan **plus** a refreshed suggestion, leaving the target in force unchanged until the member confirms it (FR-009)
- [ ] T042 [US1] Create `SetDietTargetsHandler` in `OpenMind.Healthcare/backend/DietApi/Features/DietPlan/SetTargets/SetDietTargetsHandler.cs` — the only path that changes targets
- [ ] T043 [US1] Create `OpenMind.Healthcare/backend/DietApi/Features/DietPlan/DietPlanEndpoints.cs` as a static class with `MapDietPlanEndpoints`, using `MapGroup("/api/diet-plan").WithTags("DietPlan").RequireAuthorization()` and per-route `.WithName().WithOpenApi()`. Delegates translate `DomainException` to `Results.BadRequest(new { message = ex.Message })` and a missing plan to `Results.NotFound()`, and contain no other logic
- [ ] T044 [US1] Register `IDietPlanRepository`, `TargetSuggestionService`, and `MapDietPlanEndpoints()` in `OpenMind.Healthcare/backend/DietApi/Program.cs`
- [ ] T045 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/DietPlanHandlerTests.cs` covering, for each of the five handlers, the success path and the unauthenticated path using `SignedInUser.Anonymous()`, plus the duplicate-plan rejection and the below-floor warning path

### Front end for User Story 1

- [ ] T046 [P] [US1] Create the diet setup component at `OpenMind.Healthcare/frontend/src/app/components/diet-setup/` (ts, html, css) with a reactive form for goal, start date, height, age, sex, current weight, activity level, and optional target weight; it requests a suggestion, displays it with the resting-energy and activity figures behind it and the not-medical-advice line, and lets the member accept or override
- [ ] T047 [US1] Add the plan methods to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`: get plan, request suggestion, create plan, update plan, set targets
- [ ] T048 [US1] Declare `DietSetupComponent` and add a `diet/setup` route guarded by `AuthGuard` in `OpenMind.Healthcare/frontend/src/app/app.module.ts`

**Checkpoint**: User Story 1 is fully functional. A member can create, view, and revise a plan with
a calculated, overridable target. Quickstart scenario V1 passes.

---

## Phase 4: User Story 2 - Log What I Ate and See Where the Day Stands (Priority: P2)

**Goal**: A member searches the curated library, logs meals against a date, and sees the day's
running total against target update immediately.

**Independent Test**: With a plan in place, add entries across meals, confirm totals and remaining
update after each; edit and delete entries and confirm totals adjust; delete every entry and confirm
the day returns to **not logged**, not a zero-calorie on-target day; search for an absent food and
confirm nothing is created.

**Depends on**: US1 — a target must exist before a day can be assessed against it.

### Food library for User Story 2

- [ ] T049 [P] [US2] Create `ServingSize` entity in `OpenMind.Healthcare/backend/DietApi/Domain/Entities/ServingSize.cs` with `Label`, `GramWeight`, and an owned `NutritionValues` **for that serving** — not per 100 g (research.md R-009)
- [ ] T050 [US2] Create the `FoodLibraryItem` aggregate in `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/FoodLibraryItem.cs` with `Name`, an indexed lowercased accent-stripped `SearchName`, `Category`, and an owned `ServingSizes` collection requiring at least one entry
- [ ] T051 [US2] Configure `FoodLibraryItem` and its owned `ServingSizes` in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DietDbContext.cs` `OnModelCreating`, add the `FoodLibraryItems` DbSet, and index `SearchName`
- [ ] T052 [US2] Create `IFoodLibraryRepository` in `OpenMind.Healthcare/backend/DietApi/Domain/Repositories/IFoodLibraryRepository.cs` and `FoodLibraryRepository` in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Repositories/FoodLibraryRepository.cs`, implementing search as a case-insensitive prefix-then-substring match on `SearchName`, ordered prefix matches first then alphabetically, capped at 20 results (research.md R-009)
- [ ] T053 [US2] Add a guarded `SeedFoodLibrary` to `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DbInitializer.cs` with 150-200 common foods across the eight categories, each with realistic serving sizes and nutrition values. This is what SC-004's "85% of common foods found" rests on — if it proves thin during validation, widen the seed rather than relax the criterion
- [ ] T054 [P] [US2] Create `OpenMind.Healthcare/backend/DietApi/Features/FoodLibrary/FoodLibraryDtos.cs`, the `SearchFoods` and `GetFood` handlers in their own folders, and `FoodLibraryEndpoints.cs` on `/api/food-library`. An empty `matches` array is how the contract says a food is unavailable (FR-022)

### Logging domain for User Story 2

- [ ] T055 [P] [US2] Create `FoodEntry` entity in `OpenMind.Healthcare/backend/DietApi/Domain/Entities/FoodEntry.cs` holding `FoodLibraryItemId` and `ServingSizeId` for provenance, snapshotted `FoodName` and `ServingLabel`, `Quantity`, `MealType`, an owned `NutritionValues`, and `LoggedAt`
- [ ] T056 [P] [US2] Create `DayAssessment` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/DayAssessment.cs` with date, consumed, target, remaining (negative when over), `DayState`, and overage
- [ ] T057 [US2] Create `OpenMind.Healthcare/backend/DietApi/Domain/Rules/FoodEntryRules.cs` with `EntryDateCannotBeInFutureRule`, `EntryDateCannotPrecedePlanStartRule`, `QuantityMustBePositiveRule`, and `EntryCaloriesWithinCeilingRule` (10,000 kcal)
- [ ] T058 [US2] Create the `LoggedDay` aggregate in `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/LoggedDay.cs` as its **own aggregate root**, not a collection owned by `DietPlan` (research.md R-004 and plan.md Complexity Tracking). It holds `DietPlanId`, a denormalised `UserId`, `Date`, an immutable `TargetSnapshot` captured at creation (R-006), persisted `Totals`, and an owned `Entries` collection. Methods: `StartDay`, `AddEntry` (multiplying the serving's nutrition by quantity and snapshotting it — R-005), `UpdateEntry`, `RemoveEntry`, `IsEmpty`, `Assess`, and `EntriesByMeal`. Every mutation recomputes `Totals`
- [ ] T059 [P] [US2] Create the `FoodEntryLoggedEvent` domain event in `OpenMind.Healthcare/backend/DietApi/Domain/Events/`
- [ ] T060 [US2] Configure `LoggedDay` and its owned `FoodEntries` in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DietDbContext.cs` `OnModelCreating` with a unique index on `(DietPlanId, Date)`, a supporting index on `(UserId, Date)` for range reads, calorie columns as `int` and macro columns with `HasPrecision`, and add the `LoggedDays` DbSet
- [ ] T061 [US2] Create `ILoggedDayRepository` in `OpenMind.Healthcare/backend/DietApi/Domain/Repositories/ILoggedDayRepository.cs` and `LoggedDayRepository` in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Repositories/LoggedDayRepository.cs` with `GetByDateAsync`, `GetRangeAsync` (returning day summaries only, never entries), `GetByEntryIdAsync`, `AddAsync`, `UpdateAsync`, and `DeleteAsync` for the empty-day case
- [ ] T062 [US2] Generate the migration `AddFoodLibraryAndLogging` into `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Migrations/`

### Tests for User Story 2

- [ ] T063 [P] [US2] Create `OpenMind.Healthcare/backend/DietApi.Tests/TestSupport/FakeLoggedDayRepository.cs` and `FakeFoodLibraryRepository.cs` as in-memory doubles with a `SaveCount`
- [ ] T064 [P] [US2] Create `OpenMind.Healthcare/backend/DietApi.Tests/TestSupport/LoggedDayBuilder.cs` building a day around a pinned clock with fluent entry seeding
- [ ] T065 [P] [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/FoodEntryRulesTests.cs` proving each of the four rules throws when broken, including the date-before-plan-start and calorie-ceiling boundaries
- [ ] T066 [P] [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/LoggedDayTotalsTests.cs` asserting the denormalisation invariant directly — after every `AddEntry`, `UpdateEntry`, and `RemoveEntry`, `Totals.Calories` equals the sum of entry calories (research.md R-010). This test is the reason the stored total is safe
- [ ] T067 [P] [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/DayAssessmentTests.cs` covering on-target at exactly the target, over-target with the right overage, fractional quantities, and a day whose target snapshot differs from the plan's current target
- [ ] T068 [P] [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/EmptyDayTests.cs` proving that removing the last entry leaves `IsEmpty` true so the day is deleted and the date reverts to `NotLogged` rather than becoming a zero-calorie compliant day (research.md R-008)

### Endpoints for User Story 2

- [ ] T069 [P] [US2] Create `OpenMind.Healthcare/backend/DietApi/Features/FoodLog/FoodLogDtos.cs` per the contract
- [ ] T070 [P] [US2] Create `GetDayHandler` in `OpenMind.Healthcare/backend/DietApi/Features/FoodLog/GetDay/GetDayHandler.cs`, returning a zeroed `NotLogged` day for a date with no entries rather than a 404
- [ ] T071 [US2] Create `AddFoodEntryHandler` in `OpenMind.Healthcare/backend/DietApi/Features/FoodLog/AddFoodEntry/AddFoodEntryHandler.cs`, creating the day with the plan's current targets snapshotted onto it when this is the date's first entry, and returning the full day so the client updates totals in one round trip (SC-005)
- [ ] T072 [US2] Create `UpdateFoodEntryHandler` in `OpenMind.Healthcare/backend/DietApi/Features/FoodLog/UpdateFoodEntry/UpdateFoodEntryHandler.cs`, re-reading nutrition for the new serving and re-snapshotting it
- [ ] T073 [US2] Create `DeleteFoodEntryHandler` in `OpenMind.Healthcare/backend/DietApi/Features/FoodLog/DeleteFoodEntry/DeleteFoodEntryHandler.cs`, deleting the day when its last entry goes
- [ ] T074 [US2] Create `OpenMind.Healthcare/backend/DietApi/Features/FoodLog/FoodLogEndpoints.cs` on `/api/food-log` with the four routes from the contract
- [ ] T075 [US2] Register the food-log and food-library repositories and both endpoint groups in `OpenMind.Healthcare/backend/DietApi/Program.cs`
- [ ] T076 [P] [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/FoodLogHandlerTests.cs` and `FoodLibraryHandlerTests.cs` covering the success and unauthenticated paths for all six handlers, plus adding an entry with no plan, a future date, and a food id that does not exist
- [ ] T077 [P] [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/NutritionSnapshotTests.cs` proving that changing a library item's nutrition after an entry was logged does not alter the logged entry or its day's totals (FR-025, SC-009)

### Front end for User Story 2

- [ ] T078 [P] [US2] Create the food search component at `OpenMind.Healthcare/frontend/src/app/components/food-search/` with debounced type-ahead against the search endpoint, serving-size selection, quantity entry, and a plain "not in our library" empty state
- [ ] T079 [P] [US2] Create the diet dashboard component at `OpenMind.Healthcare/frontend/src/app/components/diet-dashboard/` showing the day grouped by meal with consumed, remaining, and over-target state, and inline edit and delete
- [ ] T080 [US2] Add the food-log and library methods to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`
- [ ] T081 [US2] Declare both components and add the `diet` and `diet/log/:date` routes guarded by `AuthGuard` in `OpenMind.Healthcare/frontend/src/app/app.module.ts`

**Checkpoint**: Users Stories 1 and 2 both work. The daily loop is complete. Quickstart V2 passes.

---

## Phase 5: User Story 3 - Review My History and Consistency (Priority: P3)

**Goal**: A calendar of marked days for a month or a year, with current streak, longest streak, days
logged, and average intake.

**Independent Test**: Seed a mixed history, open the calendar for the covering month and year, and
confirm every day's marking and every statistic matches — including that lowering the plan's target
afterwards leaves already-assessed days unchanged.

**Depends on**: US2 for logged data.

- [ ] T082 [P] [US3] Create `DietStatistics` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/DietStatistics.cs` with current streak, longest streak, total days logged, average daily calories, and the averaging window
- [ ] T083 [US3] Create `StreakCalculator` in `OpenMind.Healthcare/backend/DietApi/Domain/Services/StreakCalculator.cs` taking an ordered list of `(DateOnly, DayState)`, the plan start date, and `asOf`. The current streak counts back from `asOf`'s date through consecutive `OnTarget` days and stops at the first day that is `OverTarget` **or** `NotLogged`; days before plan start or after `asOf` are excluded entirely; the average is over the most recent 30 logged days (research.md R-008, FR-036)
- [ ] T084 [P] [US3] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/StreakCalculatorTests.cs` covering a broken streak, a single-day streak, an all-unlogged history, a streak interrupted by an unlogged day rather than an over-target one, a range spanning February 29, and days before plan start being excluded from every figure
- [ ] T085 [P] [US3] Create `OpenMind.Healthcare/backend/DietApi/Features/FoodLog/GetDayRange/GetDayRangeHandler.cs` returning one summary row per day — never entries — and marking days outside the plan as `OutsidePlan` so they render as neither success nor miss (FR-036)
- [ ] T086 [P] [US3] Create `OpenMind.Healthcare/backend/DietApi/Features/DietStats/DietStatsDtos.cs`, `GetDietStats/GetDietStatsHandler.cs`, and `DietStatsEndpoints.cs` on `/api/diet-stats`, returning zeros rather than an error for a member with a plan and no entries (FR-037)
- [ ] T087 [US3] Register `MapDietStatsEndpoints()` and add the range route to `FoodLogEndpoints.cs`, then wire both in `OpenMind.Healthcare/backend/DietApi/Program.cs`
- [ ] T088 [P] [US3] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/DietStatsHandlerTests.cs` and `GetDayRangeHandlerTests.cs` covering success, unauthenticated, and the empty-history path
- [ ] T089 [P] [US3] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/TargetSnapshotTests.cs` proving that lowering the plan's calorie target does not flip previously assessed days from on-target to over-target (FR-004, SC-009) — the single most valuable regression test in this feature
- [ ] T090 [P] [US3] Create the diet calendar component at `OpenMind.Healthcare/frontend/src/app/components/diet-calendar/` with month and year views sharing one marking function, plus a statistics panel
- [ ] T091 [US3] Add the day-range and statistics methods to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`, and declare the component with a `diet/calendar` route in `app.module.ts`

**Checkpoint**: History and consistency are visible. Quickstart V3 passes.

---

## Phase 6: User Story 4 - Watch My Weight Move Toward the Goal (Priority: P4)

**Goal**: Dated weight readings, a trend over a chosen period, change since plan start, and distance
to target weight.

**Independent Test**: Record readings across dates, confirm the trend is date-ordered with correct
change and remaining figures, confirm a second reading for a date replaces the first, and confirm the
newest reading is what a refreshed target suggestion uses.

**Depends on**: US1, which already built `WeightReading` and `RecordWeight`. This phase adds the
trend, the endpoints, and the UI.

- [ ] T092 [P] [US4] Create `WeightTrend` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/WeightTrend.cs` with ordered readings, start weight, current weight, change, target weight, remaining to target, and `GoalReached`
- [ ] T093 [US4] Add `WeightTrend(from, to, asOf)` to the `DietPlan` aggregate in `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/DietPlan.cs`, computing change from the reading nearest the plan start date and flagging the goal reached per goal direction (FR-015, FR-016)
- [ ] T094 [P] [US4] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/WeightTrendTests.cs` covering change since start, remaining to target, goal reached for both a loss and a gain goal, a plan with no target weight, and an empty reading set returning an empty trend rather than throwing
- [ ] T095 [P] [US4] Create `OpenMind.Healthcare/backend/DietApi/Features/Weight/WeightDtos.cs` and the `GetWeightTrend`, `RecordWeight`, and `DeleteWeightReading` handlers, each in its own folder
- [ ] T096 [US4] Create `OpenMind.Healthcare/backend/DietApi/Features/Weight/WeightEndpoints.cs` on `/api/weight` with `GET /`, `PUT /{date}`, and `DELETE /{date}`, and register it in `Program.cs`
- [ ] T097 [P] [US4] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/WeightHandlerTests.cs` covering success and unauthenticated paths for all three handlers, plus the future-date and implausible-weight rejections and the same-date replacement
- [ ] T098 [P] [US4] Create the weight tracker component at `OpenMind.Healthcare/frontend/src/app/components/weight-tracker/` with a reading entry form, a simple trend chart, and the change and remaining figures
- [ ] T099 [US4] Add the weight methods to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`, and declare the component with a `diet/weight` route in `app.module.ts`

**Checkpoint**: The weight loop closes with the target suggestion. Quickstart V4 passes.

---

## Phase 7: User Story 5 - Earn Recognition for Sticking With It (Priority: P5)

**Goal**: Named achievements unlock on thresholds, are stamped with an earned date, and are never
revoked.

**Independent Test**: Seed history to just below a threshold, log the qualifying day, and confirm the
achievement unlocks with the right date. Then delete entries so the day no longer qualifies and
confirm the achievement **stays** unlocked.

**Depends on**: US3 for the statistics achievements are evaluated against.

- [ ] T100 [P] [US5] Create the `DietAchievement` aggregate in `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/DietAchievement.cs` with name, description, icon, `AchievementCriterion`, and threshold
- [ ] T101 [P] [US5] Create `UnlockedAchievement` entity in `OpenMind.Healthcare/backend/DietApi/Domain/Entities/UnlockedAchievement.cs` with `DietPlanId`, `DietAchievementId`, and `EarnedOn`
- [ ] T102 [US5] Add the owned `UnlockedAchievements` collection and an `Unlock(achievementId, earnedOn)` method to `DietPlan` in `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/DietPlan.cs`, silently doing nothing when the achievement is already unlocked so it can never duplicate or revoke (FR-039)
- [ ] T103 [US5] Create `DietAchievementStatusService` in `OpenMind.Healthcare/backend/DietApi/Domain/Services/DietAchievementStatusService.cs`. Persisted state wins: if the plan holds an `UnlockedAchievement`, it is unlocked regardless of current statistics. Otherwise evaluate the criterion and unlock when met. Locked entries carry the remaining count. This deliberately differs from the smoking area's derived `AchievementStatusService`, which cannot satisfy FR-039 (research.md R-007)
- [ ] T104 [US5] Configure `DietAchievement` and the owned `UnlockedAchievements` in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DietDbContext.cs` `OnModelCreating` with a unique index on `(DietPlanId, DietAchievementId)`, add the `DietAchievements` DbSet, and generate the migration `AddDietAchievements`
- [ ] T105 [US5] Add a guarded `SeedDietAchievements` to `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DbInitializer.cs` with the eight definitions from [data-model.md](./data-model.md): first day logged, 7, 14 and 30 consecutive on-target days, 30 and 100 total days logged, and 30 and 100 days on plan
- [ ] T106 [P] [US5] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/DietAchievementTests.cs` proving an achievement unlocks once with the right earned date, that a second evaluation awards nothing, that a previously earned achievement survives statistics falling back below its threshold, and that locked entries report the correct remaining count
- [ ] T107 [P] [US5] Create `OpenMind.Healthcare/backend/DietApi/Features/DietAchievements/DietAchievementDtos.cs`, the `GetAllDietAchievements`, `GetUnlockedDietAchievements`, and `CheckNewDietAchievements` handlers, and `DietAchievementsEndpoints.cs` on `/api/diet-achievements`, then register it in `Program.cs`
- [ ] T108 [P] [US5] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/DietAchievementHandlerTests.cs` covering success and unauthenticated paths, plus calling check twice and confirming the second call awards nothing
- [ ] T109 [P] [US5] Create the achievements component at `OpenMind.Healthcare/frontend/src/app/components/diet-achievements/`, add its service methods, and declare it with a `diet/achievements` route in `app.module.ts`

**Checkpoint**: Recognition works and cannot be taken away. Quickstart V5 passes.

---

## Phase 8: User Story 6 - Get Guidance When I Need It (Priority: P6)

**Goal**: Curated eating tips and a progress-aware encouragement message.

**Independent Test**: Open guidance with an active plan and confirm tips come from the seeded library
and the encouragement reflects current progress; a member with no logged days gets a getting-started
message rather than an error.

- [ ] T110 [P] [US6] Create the `EatingTip` aggregate in `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/EatingTip.cs` with title, description, icon, and `TipCategory`
- [ ] T111 [US6] Configure `EatingTip` in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DietDbContext.cs` `OnModelCreating`, add the `EatingTips` DbSet, and generate the migration `AddEatingTips`
- [ ] T112 [US6] Add a guarded `SeedEatingTips` to `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DbInitializer.cs` covering the five tip categories, written as general wellbeing guidance and never as clinical advice (spec Assumptions)
- [ ] T113 [P] [US6] Create `OpenMind.Healthcare/backend/DietApi/Features/DietGuidance/DietGuidanceDtos.cs`, the `GetEatingTips` and `GetDailyEncouragement` handlers, and `DietGuidanceEndpoints.cs` on `/api/diet-guidance`, then register it in `Program.cs`
- [ ] T114 [P] [US6] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/DietGuidanceHandlerTests.cs` covering success, unauthenticated, the streak-aware message, and the no-logged-days getting-started path
- [ ] T115 [P] [US6] Create the guidance component at `OpenMind.Healthcare/frontend/src/app/components/diet-guidance/`, add its service methods, and declare it with a `diet/guidance` route in `app.module.ts`

**Checkpoint**: All six user stories are independently functional.

---

## Phase 9: Polish & Cross-Cutting Concerns

- [ ] T116 [P] Add diet navigation to `OpenMind.Healthcare/frontend/src/app/components/navbar/` so the new area is reachable alongside the smoking-cessation areas
- [ ] T117 Delete `OpenMind.Healthcare/backend/DietApi/diet.db` and start `DietApi` twice, confirming the second start adds no duplicate foods, tips, or achievement definitions (SC-011, Principle VI)
- [ ] T118 Seed roughly three years of daily entries for one member and time the calendar year view, the weight trend, and the statistics endpoint. Each must return under a second (SC-006). If this fails, the aggregate split in research.md R-004 and the stored per-day totals in R-010 need revisiting — not the success criterion
- [ ] T119 Run `docker compose up --build` from `OpenMind.Healthcare/` and exercise the diet area through <http://localhost:5435>. This is the only run that catches a `/diet-api` prefix added to `proxy.conf.json` but not `nginx.conf`
- [ ] T120 Stop `QuitSmokingApi` and confirm every diet capability still works, then stop `DietApi` and confirm the smoking area still works, verifying the three database files and volumes are separate and no diet table holds a cross-service foreign key (FR-044, Principle I)
- [ ] T121 Work through every scenario in [quickstart.md](./quickstart.md) V1 to V8 and record the results
- [ ] T122 Run the constitution's gates: `dotnet build OpenMind.Healthcare.sln` with no new warnings, `dotnet test OpenMind.Healthcare.sln` passing and actually executing the diet tests, `npm run build` in `OpenMind.Healthcare/frontend/`, and confirm every new endpoint appears in Scalar and requires authorization
- [ ] T123 Confirm the safe-floor calorie figures in `OpenMind.Healthcare/backend/DietApi/Domain/Services/TargetSuggestionService.cs` with whoever owns clinical content before release. The 1,200 and 1,500 values are working defaults taken from commonly published guidance, flagged for review in both spec Assumptions and research.md R-002 — this is the one number in the feature with a genuine duty of care attached

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — **blocks every user story**
- **US1 (Phase 3)**: Depends on Foundational. No dependency on any other story
- **US2 (Phase 4)**: Depends on US1 — a day cannot be assessed without a target
- **US3 (Phase 5)**: Depends on US2 — there is no history without logged days
- **US4 (Phase 6)**: Depends on US1 only. `WeightReading` and `RecordWeight` already exist from T022 and T025, so this phase can run **in parallel with US2 and US3**
- **US5 (Phase 7)**: Depends on US3 for the statistics its criteria are evaluated against
- **US6 (Phase 8)**: Depends on US1 only for the plan; can run in parallel with US3, US4, and US5
- **Polish (Phase 9)**: Depends on every story that is being shipped

### Story Dependency Graph

```text
Setup → Foundational → US1 ─┬─→ US2 → US3 → US5
                            ├─→ US4
                            └─→ US6
```

This feature's stories are **not** all mutually independent, and the plan says so rather than
pretending otherwise: the spec itself notes that logging is meaningful only once a target exists.
US4 and US6 are the genuinely parallel branches.

### Within Each User Story

- Value objects and entities before aggregates
- Aggregates before repositories and EF configuration
- EF configuration before the migration
- Domain tests alongside the domain, before handlers
- Handlers before endpoints
- Endpoints before the front end

### Parallel Opportunities

- All of T004-T009 in Setup run together — different files, no shared state
- T010, T011, T012, T017, T018, T019 in Foundational run together
- Within US1: T020, T021, T022 together; then T031-T036 (test support and domain tests) together
- Within US2: T049 and T055, T056 together; T063-T068 together
- Across stories: once US1 is done, one developer can take US2→US3→US5 while another takes US4 and a
  third takes US6

---

## Parallel Example: User Story 1

```bash
# Value objects and the weight entity - three different files, no shared dependency:
Task: "Create BodyMetrics value object in Domain/ValueObjects/BodyMetrics.cs"
Task: "Create TargetSuggestion value object in Domain/ValueObjects/TargetSuggestion.cs"
Task: "Create WeightReading entity in Domain/Entities/WeightReading.cs"

# Once the aggregate exists, all domain tests and test support run together:
Task: "Create FakeDietPlanRepository in TestSupport/"
Task: "Create DietPlanBuilder in TestSupport/"
Task: "Write DietPlanRulesTests covering all seven rules"
Task: "Write TargetSuggestionTests covering both sexes, five activity levels, four goals, floor clamp"
Task: "Write WeightRecordingTests for same-date replacement and CurrentWeightKg"
Task: "Write TargetChangeTests for UpdatePlan leaving targets untouched"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup — T001 to T009
2. Phase 2 Foundational — T010 to T019 (**blocks everything**)
3. Phase 3 User Story 1 — T020 to T048
4. **STOP and VALIDATE**: run quickstart scenario V1
5. Deploy or demo — a member can create a plan with a calculated, overridable daily target

The MVP is 48 tasks. It is genuinely useful on its own: it answers "what should I be eating each
day", which is the question a member cannot answer for themselves.

### Incremental Delivery

1. Setup + Foundational → the service exists and is wired in
2. + US1 → **MVP**: a personalised, overridable daily target
3. + US2 → the daily loop: search, log, see the day stand
4. + US3 → consistency: calendar, streaks, averages
5. + US4 → the payoff: weight moving toward the goal
6. + US5 → recognition
7. + US6 → guidance
8. + Polish → validated, containerised, gates green

Each step ships without breaking the one before it.

### Parallel Team Strategy

With three developers, after Setup + Foundational + US1 are done together:

- Developer A: US2 → US3 → US5 (the logging spine)
- Developer B: US4 (weight)
- Developer C: US6 (guidance), then the front-end polish in Phase 9

---

## Notes

- `[P]` marks tasks touching different files with no dependency on incomplete work
- `[Story]` labels map each task to a user story for traceability; Setup, Foundational, and Polish
  carry no story label by design
- Test tasks are required here, not optional — Principle V makes domain and slice tests part of the
  feature, and the constitution's gates check them
- Commit after each task or logical group; stop at any checkpoint to validate a story independently
- The three decisions most likely to be second-guessed during implementation are the aggregate split
  (R-004), the integer calorie columns with stored per-day totals (R-010), and persisted achievement
  unlocks (R-007). Each is justified in plan.md Complexity Tracking with what was rejected and why —
  read that before changing any of them
