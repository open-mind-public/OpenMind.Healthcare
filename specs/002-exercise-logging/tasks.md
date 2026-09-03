---

description: "Task list for Exercise Logging implementation"
---

# Tasks: Exercise Logging

**Input**: Design documents from `/specs/002-exercise-logging/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Test tasks are **required**, not optional. Constitution Principle V makes domain tests
(every `IBusinessRule` proven to throw, calculations covered at boundary values) and slice tests
(success path and unauthenticated path per handler) part of the feature.

**Organization**: Tasks are grouped by user story. Each story is a deployable increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1-US4)
- Paths are repository-relative from `c:\Users\tung.le\Data\git-personal\OpenMind.Healthcare`

## Path Conventions

Additive inside the existing diet service — no new project, port, volume or compose service:

- Backend: `OpenMind.Healthcare/backend/DietApi/`
- Tests: `OpenMind.Healthcare/backend/DietApi.Tests/`
- Front end: `OpenMind.Healthcare/frontend/src/app/`

---

## Phase 1: Setup

**Purpose**: The primitives every later type references.

Deliberately short. This feature allocates no port, volume, database file or frontend prefix and
adds no project — it lands inside `DietApi`, which already has all of those (research.md R-001).

- [X] T001 [P] Create `ActivityCategory` enum in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/ActivityCategory.cs` with `Walking`, `Running`, `Cycling`, `Swimming`, `Gym`, `Sport`, `HomeAndGarden`, `Everyday`
- [X] T002 [P] Create `ExerciseTotals` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/ExerciseTotals.cs` with `int Minutes` and `int Kilocalories`, static `Zero`/`Create` factories, a `Plus` method and `GetEqualityComponents()`. Both are `int` because the weekly summary aggregates them in SQL, and EF Core maps `decimal` to SQLite `TEXT` (ADR 0002)
- [X] T003 [P] Create the `ExerciseLoggedEvent` domain event in `OpenMind.Healthcare/backend/DietApi/Domain/Events/ExerciseEvents.cs`

**Checkpoint**: Shared types exist; the service still builds unchanged.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The activity catalogue and the persistence plumbing every story needs before it can
store or read anything.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Create the `ActivityType` aggregate in `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/ActivityType.cs` with `Name`, an indexed lowercased accent-stripped `SearchName`, `Category` and a `decimal Met`. Reuse the normalisation helper pattern from `FoodLibraryItem` so "crème" style names are searchable
- [X] T005 Create `IActivityTypeRepository` in `OpenMind.Healthcare/backend/DietApi/Domain/Repositories/IActivityTypeRepository.cs` and `ActivityTypeRepository` in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Repositories/ActivityTypeRepository.cs`, with `GetByIdAsync` and a `SearchAsync` doing case-insensitive prefix-then-substring matching on `SearchName`, prefix matches first then alphabetically, capped at 20
- [X] T006 Create `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Seeds/ActivityCatalogueSeed.cs` with 60-80 activities across the eight categories, each carrying a MET value from the Compendium of Physical Activities, with intensity as separate entries ("Running, 8 km/h" and "Running, 12 km/h" are two rows, not one row plus a field — research.md R-003). Note the compendium edition used in a file comment
- [X] T007 Add a guarded `SeedActivityCatalogue` block to `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DbInitializer.cs` following the existing `if (!context.X.Any())` pattern, keeping the single `SaveChanges()` at the end
- [X] T008 Configure `ExerciseDay`, its owned `ExerciseEntries` and `ActivityType` in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DietDbContext.cs` `OnModelCreating`: add the `ExerciseDays` and `ActivityTypes` DbSets; unique index on `(DietPlanId, Date)`; supporting index on `(UserId, Date)` for range reads; index on `SearchName`; `Version` marked `.IsConcurrencyToken()`; minute and kilocalorie columns as `int`, `Met` with `HasPrecision(4, 1)`; enums via `HasConversion<string>().HasMaxLength(50)`; `Ignore(e => e.DomainEvents)` on every mapped entity and `UsePropertyAccessMode(PropertyAccessMode.Field)` on the owning navigation
- [X] T009 Generate the migration with `dotnet ef migrations add AddExerciseLogging -o Infrastructure/Data/Migrations` from `OpenMind.Healthcare/backend/DietApi`, and verify it produces a working schema against an empty database
- [X] T010 [P] Create `OpenMind.Healthcare/backend/DietApi.Tests/TestSupport/FakeActivityTypeRepository.cs` as an in-memory double, exposing a few known activities (a run at 8.3 MET, a brisk walk at 4.3 MET, and one implausibly intense entry for ceiling tests)
- [X] T011 [P] Create `OpenMind.Healthcare/backend/DietApi.Tests/TestSupport/FakeExerciseDayRepository.cs` as an in-memory `IExerciseDayRepository` with a `SaveCount` and a `DeleteCount`, mirroring `FakeLoggedDayRepository`
- [X] T012 [P] Add exercise wire types to `OpenMind.Healthcare/frontend/src/app/models/diet.models.ts` for every response shape in [contracts/rest-api.md](./contracts/rest-api.md)

**Checkpoint**: The catalogue is seeded and searchable, the schema exists, and the fakes are ready.

---

## Phase 3: User Story 1 - Record That I Exercised (Priority: P1) 🎯 MVP

**Goal**: A member picks an activity, says how long it lasted, and sees it recorded against a date
with an estimate of the energy used.

**Independent Test**: With a plan in place, record 45 minutes of running for today, reload, and
confirm it persists with an estimate. Record a second session and confirm both are kept. Try a zero
duration, a future date and a pre-plan date and confirm all three are refused.

**Depends on**: Phase 2 — the catalogue must exist before anything can be logged against it.

### Domain for User Story 1

- [X] T013 [P] [US1] Create `ExerciseEntry` in `OpenMind.Healthcare/backend/DietApi/Domain/Entities/ExerciseEntry.cs` holding `ActivityTypeId` for provenance, a snapshotted `ActivityName` and `Met`, `DurationMinutes`, `EstimatedKcal` and `RecordedAt`. The snapshot is what stops a corrected MET value rewriting a member's history (FR-009)
- [X] T014 [P] [US1] Create `EnergyEstimator` in `OpenMind.Healthcare/backend/DietApi/Domain/Services/EnergyEstimator.cs` computing `met × weightKg × (durationMinutes / 60)`, rounded away from zero with a floor of 1 kcal so a recorded session never displays "0 kcal" (research.md R-003, R-010). Pure — no clock, no dependencies
- [X] T015 [US1] Create `OpenMind.Healthcare/backend/DietApi/Domain/Rules/ExerciseEntryRules.cs` implementing `IBusinessRule` for `ExerciseDateCannotBeInFutureRule`, `ExerciseDateCannotPrecedePlanStartRule`, `DurationMustBePositiveRule` and `DurationWithinCeilingRule` (1,440 minutes), each naming itself via `nameof` and taking the comparison instant as a parameter rather than reading the clock
- [X] T016 [US1] Create the `ExerciseDay` aggregate in `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/ExerciseDay.cs` as **its own aggregate root**, not part of `LoggedDay` — `LoggedDay` is deleted when its last meal is removed, so exercise living there would vanish with the dinner and would have nowhere to live on a food-free day (research.md R-002, FR-013). It holds `DietPlanId`, a denormalised `UserId`, an immutable `Date`, persisted `Totals`, a `Guid Version` and an owned `Entries` collection. Methods: `StartDay`, `AddEntry`, `UpdateEntry`, `RemoveEntry`, `IsEmpty`, `EntriesInOrder`. Every mutation recomputes `Totals` **and** reassigns `Version`
- [X] T017 [US1] Create `IExerciseDayRepository` in `OpenMind.Healthcare/backend/DietApi/Domain/Repositories/IExerciseDayRepository.cs` and `ExerciseDayRepository` in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Repositories/ExerciseDayRepository.cs` with `GetByDateAsync`, `GetByEntryIdAsync` (filtering by `UserId` so another member's entry is unreachable rather than merely forbidden), `GetRangeAsync` returning summaries only, `AddAsync`, `UpdateAsync` letting `DbUpdateConcurrencyException` escape, and `DeleteAsync` for the empty-day case

### Tests for User Story 1

- [X] T018 [P] [US1] Create `OpenMind.Healthcare/backend/DietApi.Tests/TestSupport/ExerciseDayBuilder.cs` building a day around a single pinned `Clock` that tests pass back as `asOf`, with fluent methods for the date, the plan start and seeded sessions
- [X] T019 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/ExerciseEntryRulesTests.cs` proving each of the four rules throws when broken and passes at its boundary values, including a session on the plan start date and one of exactly 1,440 minutes
- [X] T020 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/EnergyEstimatorTests.cs` pinning the worked example from research.md R-003 (8.3 MET, 70 kg, 45 min → 436 kcal), proving the estimate scales with body weight, and proving the 1 kcal floor on a one-minute gentle session
- [X] T021 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/ExerciseDayTotalsTests.cs` asserting the denormalisation invariant directly — after every `AddEntry`, `UpdateEntry` and `RemoveEntry`, `Totals.Minutes` and `Totals.Kilocalories` equal the sums over the entries — and that each mutation reassigns `Version`
- [X] T022 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/MultipleSessionsTests.cs` proving a second session on the same date is added rather than replacing the first, and that the day's totals are their sum (FR-004)

### Endpoints for User Story 1

- [X] T023 [P] [US1] Create `OpenMind.Healthcare/backend/DietApi/Features/ActivityCatalogue/ActivityCatalogueDtos.cs`, the `SearchActivities` handler in its own folder, and `ActivityCatalogueEndpoints.cs` on `/api/activity-catalogue` with `.RequireAuthorization()`. An empty `matches` array is how the contract says an activity is unavailable (FR-027)
- [X] T024 [P] [US1] Create `OpenMind.Healthcare/backend/DietApi/Features/Exercise/ExerciseDtos.cs` with the request and response records from [contracts/rest-api.md](./contracts/rest-api.md), including the nullable `version` on a day that does not exist yet
- [X] T025 [US1] Create `GetExerciseDayHandler` in `OpenMind.Healthcare/backend/DietApi/Features/Exercise/GetExerciseDay/GetExerciseDayHandler.cs`, returning an empty day for a date with nothing recorded rather than a 404, refusing out-of-plan dates, and returning null when the member has no plan so the endpoint can answer 404
- [X] T026 [US1] Create `AddExerciseEntryHandler` in `OpenMind.Healthcare/backend/DietApi/Features/Exercise/AddExerciseEntry/AddExerciseEntryHandler.cs`, creating the day when it is the date's first entry, resolving the activity from the catalogue, reading the member's current weight from their plan, computing the estimate and snapshotting it onto the entry, and returning the full day so the client updates totals in one round trip (SC-002)
- [X] T027 [US1] Create `OpenMind.Healthcare/backend/DietApi/Features/Exercise/ExerciseEndpoints.cs` on `/api/exercise` with the day and add-entry routes, translating `DomainException` to 400, a missing plan to 404 and `ConcurrencyConflictException` to 409, and containing no other logic
- [X] T028 [US1] Register `IExerciseDayRepository`, `IActivityTypeRepository`, `EnergyEstimator`, `MapExerciseEndpoints()` and `MapActivityCatalogueEndpoints()` in `OpenMind.Healthcare/backend/DietApi/Program.cs`
- [X] T029 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/ExerciseHandlerTests.cs` covering the success and unauthenticated paths for the get-day, add-entry and search handlers, plus logging with no plan, an unknown activity, a future date, a pre-plan date and a zero duration
- [X] T030 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/EstimateSnapshotTests.cs` proving that correcting an activity's MET value afterwards does not change an already-recorded entry's stored estimate, and that recording a new body weight does not move past estimates (FR-009, SC-007)
- [X] T031 [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/DayVerdictUnchangedTests.cs` — **the guarantee this feature exists to keep**. Record exercise against a past date that is already over target for eating, then assert that the `LoggedDay`'s target snapshot, consumed total and `DayState` are all byte-for-byte unchanged, including when the exercise is recorded days later. Repeat for an on-target day and a not-logged day (FR-015, SC-008)

### Front end for User Story 1

- [X] T032 [P] [US1] Create the exercise log component at `OpenMind.Healthcare/frontend/src/app/components/exercise-log/` (ts, html, css) with a debounced type-ahead against the catalogue, a duration field in minutes, the day's sessions with their estimates, and a plain "not in our catalogue" empty state. Use design tokens only — no raw colour values
- [X] T033 [US1] Add the exercise methods to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`: get day, search activities, add entry
- [X] T034 [US1] Add an exercise section to `OpenMind.Healthcare/frontend/src/app/components/diet-dashboard/diet-dashboard.component.html` beneath the meals, showing the day's activity, its total and the estimate, with the FR-019 line explaining that logged exercise does not change the daily target; declare `ExerciseLogComponent` in `OpenMind.Healthcare/frontend/src/app/app.module.ts`

**Checkpoint**: User Story 1 is complete. A member can record activity and see an estimate, and
quickstart V1 and V3 pass. **V3 is the one that matters.**

---

## Phase 4: User Story 2 - Correct What I Recorded (Priority: P2)

**Goal**: A member can change a session's activity or duration, or remove it, and the day updates.

**Independent Test**: Record two sessions, edit one and delete the other, and confirm both changes
survive a reload. Delete the last session and confirm the date returns to having no exercise.

**Depends on**: US1 — there must be entries to correct.

- [X] T035 [US2] Create `UpdateExerciseEntryHandler` in `OpenMind.Healthcare/backend/DietApi/Features/Exercise/UpdateExerciseEntry/UpdateExerciseEntryHandler.cs`, re-reading the activity and re-estimating from the member's current weight, then re-snapshotting — a member's own edit is deliberate, unlike a background catalogue correction
- [X] T036 [US2] Create `DeleteExerciseEntryHandler` in `OpenMind.Healthcare/backend/DietApi/Features/Exercise/DeleteExerciseEntry/DeleteExerciseEntryHandler.cs`, deleting the day when its last entry goes so the date returns to having no exercise rather than a zero-minute shell
- [X] T037 [US2] Add the update and delete routes to `OpenMind.Healthcare/backend/DietApi/Features/Exercise/ExerciseEndpoints.cs`, with `?version=` on the delete and 409 on a stale token
- [X] T038 [P] [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/EmptyExerciseDayTests.cs` proving that removing the last entry leaves `IsEmpty` true so the repository deletes the day, and that the date then reports no exercise
- [X] T039 [P] [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/ExerciseConcurrencyTests.cs` proving a stale `version` is refused with a conflict rather than overwriting, and that no entry from either session is lost (FR-012)
- [X] T040 [P] [US2] Extend `OpenMind.Healthcare/backend/DietApi.Tests/Features/ExerciseHandlerTests.cs` with the update and delete handlers' success and unauthenticated paths, another member's entry being unreachable, and editing an entry that does not exist
- [X] T041 [US2] Add edit and delete controls to `OpenMind.Healthcare/frontend/src/app/components/exercise-log/`, echoing the day `version` on every write and surfacing a 409 as a reload prompt rather than a failure
- [X] T042 [US2] Add the update and delete methods to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`

**Checkpoint**: The log is correctable. Quickstart V2 passes.

---

## Phase 5: User Story 3 - See Exercise Alongside My Eating (Priority: P3)

**Goal**: The calendar marks days with exercise without displacing the eating colour.

**Independent Test**: Seed dates with and without activity, open the calendar, and confirm the
exercise marking matches and sits beside the eating state. Confirm a day with exercise but no food
still reads as not logged for eating.

**Depends on**: US1 for data. Independent of US2.

- [X] T043 [US3] Create `GetExerciseRangeHandler` in `OpenMind.Healthcare/backend/DietApi/Features/Exercise/GetExerciseRange/GetExerciseRangeHandler.cs` returning one summary row per day **that has entries** — absence means no exercise, which is what lets the calendar mark days without inventing a state. Never loads entries
- [X] T044 [US3] Add the range route to `OpenMind.Healthcare/backend/DietApi/Features/Exercise/ExerciseEndpoints.cs`. Deliberately a **separate** endpoint from `/api/food-log`: the eating contract stays unaware of exercise (research.md R-005, FR-013)
- [X] T045 [P] [US3] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/ExerciseRangeHandlerTests.cs` covering success, unauthenticated, a member with no plan, and days outside the plan being excluded
- [X] T046 [US3] Add the range method to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts` and fetch it in parallel with the food-log range in `OpenMind.Healthcare/frontend/src/app/components/diet-calendar/diet-calendar.component.ts`, merging by date
- [X] T047 [US3] Mark exercise days in `OpenMind.Healthcare/frontend/src/app/components/diet-calendar/diet-calendar.component.html` and `.css` as an **independent indicator** — a dot or corner mark on the existing cell, not a new `DayState` member and not a replacement colour, so both facts are visible at once (research.md R-009, FR-021). Add it to the legend
- [X] T048 [US3] Extend the day view at `OpenMind.Healthcare/frontend/src/app/components/diet-dashboard/diet-dashboard.component.html` so a date opened from the calendar lists that day's activities with their durations (spec US3 scenario 3)

**Checkpoint**: Exercise and eating are visible together. Quickstart V4 passes.

---

## Phase 6: User Story 4 - See How Active I Have Been (Priority: P4)

**Goal**: A weekly count of active days and total time, comparable against the previous week.

**Independent Test**: Seed several weeks of activity, open the summary, and confirm active days,
total time and the previous-week comparison match the seeded data. A week with nothing shows zeros.

**Depends on**: US1 for data. Independent of US2 and US3.

- [X] T049 [P] [US4] Create `ActivitySummary` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/ActivitySummary.cs` with active days, total minutes, total kilocalories, the window length and the previous window's figures
- [X] T050 [US4] Create `ActivitySummaryCalculator` in `OpenMind.Healthcare/backend/DietApi/Domain/Services/ActivitySummaryCalculator.cs` taking day summaries, the plan start date and `asOf`, counting the current 7-day window and the one before it, and excluding days before the plan start or after `asOf` entirely (FR-023)
- [X] T051 [P] [US4] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/ActivitySummaryTests.cs` covering a week with no activity, a day with several sessions counting once toward active days, a range spanning a leap day, days before the plan start being excluded, and the previous-window comparison
- [X] T052 [P] [US4] Create `GetActivitySummaryHandler` in `OpenMind.Healthcare/backend/DietApi/Features/Exercise/GetActivitySummary/GetActivitySummaryHandler.cs`, returning zeros rather than an error for a member with a plan and no activity (FR-024)
- [X] T053 [US4] Add the summary route to `OpenMind.Healthcare/backend/DietApi/Features/Exercise/ExerciseEndpoints.cs`
- [X] T054 [P] [US4] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/ActivitySummaryHandlerTests.cs` covering success, unauthenticated, no plan and the empty-history path
- [X] T055 [P] [US4] Create the activity summary component at `OpenMind.Healthcare/frontend/src/app/components/activity-summary/` (ts, html, css) showing active days, total time and the week-on-week comparison, with a plain zero state
- [X] T056 [US4] Register the programme nav entry `{ path: 'activity', label: 'Activity', icon: 'zap' }` in the diet programme in `OpenMind.Healthcare/frontend/src/app/programs/programs.ts`, add the `diet/activity` route guarded by `AuthGuard` and declare `ActivitySummaryComponent` in `OpenMind.Healthcare/frontend/src/app/app.module.ts`. The shell and left rail pick it up with no further change — that is the registry working as intended
- [X] T057 [US4] Add the FR-019 explanation to `OpenMind.Healthcare/frontend/src/app/components/diet-setup/diet-setup.component.html` beside the activity level, stating that the declared level already accounts for habitual exercise and that logged sessions are a record rather than an allowance

**Checkpoint**: All four user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T058 Delete `OpenMind.Healthcare/backend/DietApi/diet.db` and start `DietApi` twice, confirming the second start adds no duplicate activities (SC-010, Principle VI). Add the assertion to `OpenMind.Healthcare/backend/DietApi.Tests/Infrastructure/SeedIdempotencyTests.cs` alongside the existing food, achievement and tip counts
- [X] T059 Check in the SC-003 judging corpus of roughly 25 everyday activity names beside the seed in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Seeds/`, chosen **independently of** the seed list, and assert the 85% first-five-results hit rate. Picking the corpus from the seed would guarantee a pass and measure nothing
- [X] T060 Extend `OpenMind.Healthcare/backend/DietApi.Tests/Infrastructure/ThreeYearHistoryTests.cs` with roughly three years of daily exercise, and time the calendar range and the summary. Each must return under the budget (SC-004). If it fails, the per-day aggregate and stored totals are what to revisit — not the criterion
- [X] T061 [P] Verify the negative guarantees in [contracts/rest-api.md](./contracts/rest-api.md) still hold, by inspecting `OpenMind.Healthcare/backend/DietApi/Features/FoodLog/FoodLogDtos.cs` and `OpenMind.Healthcare/backend/DietApi/Features/DietPlan/DietPlanDtos.cs`: no response combines the estimate with the calorie target, no food-log response carries an exercise field, and the target suggestion takes no exercise input (FR-016, FR-018, SC-009)
- [X] T062 [P] Update the API surface table in `README.md` to add `/api/exercise` and `/api/activity-catalogue` to the `DietApi` row, so the README does not go stale again
- [ ] T063 Run `docker compose up --build` from `OpenMind.Healthcare/` and exercise the feature through <http://localhost:5435>, confirming the new endpoints work behind the existing `/diet-api` prefix with no proxy or nginx change
  - **Not run**: the Docker daemon is not available on this machine. The static half is verified:
    `OpenMind.Healthcare/docker-compose.yml` needs no change (no new service, port or volume), and
    both `nginx.conf` (`/diet-api/` to `http://diet-api:5000/api/`) and the dev `proxy.conf.json`
    rewrite the whole `/api` prefix, so `/api/exercise` and `/api/activity-catalogue` are already
    covered. Only the runtime confirmation is outstanding.
- [X] T064 Work through every scenario in [quickstart.md](./quickstart.md) V1 to V6 and record the results, **V3 especially** — it is the guarantee the whole feature is shaped around
- [X] T065 Run the constitution's gates: `dotnet build OpenMind.Healthcare.sln` with no new warnings, `dotnet test OpenMind.Healthcare.sln` passing, `npm run build` in `OpenMind.Healthcare/frontend/`, and confirm the new endpoints appear in Scalar and require authorization

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — **blocks every user story**
- **US1 (Phase 3)**: Depends on Foundational. No dependency on another story
- **US2 (Phase 4)**: Depends on US1 — there must be entries to correct
- **US3 (Phase 5)**: Depends on US1 for data. Independent of US2
- **US4 (Phase 6)**: Depends on US1 for data. Independent of US2 and US3
- **Polish (Phase 7)**: Depends on every story being shipped

### Story Dependency Graph

```text
Setup → Foundational → US1 ─┬─→ US2
                            ├─→ US3
                            └─→ US4
```

US1 is the trunk: everything else needs recorded activity to act on. US2, US3 and US4 are genuinely
parallel branches once it lands.

### Within Each User Story

- Value objects and entities before aggregates
- Aggregates before repositories and EF configuration
- EF configuration before the migration
- Domain tests alongside the domain, before handlers
- Handlers before endpoints
- Endpoints before the front end

### Parallel Opportunities

- All of Phase 1 runs together — three different files
- T010, T011, T012 in Foundational run together
- Within US1: T013 and T014 together; then T018-T022 (test support and domain tests) together;
  then T023, T024 together; then T029, T030 together
- Within US2: T038, T039, T040 together
- Across stories: once US1 is done, one developer can take US2 while another takes US3 and a third
  takes US4

---

## Parallel Example: User Story 1

```bash
# Entity and estimator - different files, no shared dependency:
Task: "Create ExerciseEntry in Domain/Entities/ExerciseEntry.cs"
Task: "Create EnergyEstimator in Domain/Services/EnergyEstimator.cs"

# Once the aggregate exists, the whole domain test set runs together:
Task: "Create ExerciseDayBuilder in TestSupport/"
Task: "Write ExerciseEntryRulesTests covering all four rules"
Task: "Write EnergyEstimatorTests pinning 8.3 MET x 70 kg x 45 min = 436 kcal"
Task: "Write ExerciseDayTotalsTests asserting the totals invariant and version reassignment"
Task: "Write MultipleSessionsTests proving a second session is added, not substituted"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup — T001 to T003
2. Phase 2 Foundational — T004 to T012 (**blocks everything**)
3. Phase 3 User Story 1 — T013 to T034
4. **STOP and VALIDATE**: run quickstart V1 and **V3**
5. Deploy or demo — a member can record activity and see what it cost them

The MVP is 34 tasks. It delivers exactly what was asked for: the ability to log exercise against
dates, with an honest estimate beside it.

### Incremental Delivery

1. Setup + Foundational → the catalogue exists and is searchable
2. + US1 → **MVP**: recording activity with an estimate
3. + US2 → a log that can be corrected, and therefore trusted
4. + US3 → exercise and eating visible together
5. + US4 → the weekly picture
6. + Polish → validated, containerised, gates green

### Parallel Team Strategy

With three developers, after Setup + Foundational + US1 are done together:

- Developer A: US2 (correcting)
- Developer B: US3 (calendar)
- Developer C: US4 (summary and the new page)

---

## Notes

- `[P]` marks tasks touching different files with no dependency on incomplete work
- `[Story]` labels map each task to a user story; Setup, Foundational and Polish carry none by design
- Test tasks are required, not optional — Principle V makes domain and slice tests part of the
  feature, and the constitution's gates check them
- **T031 is the task not to skip.** The entire feature rests on exercise never moving a day's eating
  verdict; that test is what stops a well-meaning later change from quietly breaking it
- Commit after each task or logical group; stop at any checkpoint to validate a story independently
- The design reuses patterns already justified in 001 — a per-day aggregate with stored totals, a
  `Guid` concurrency token, snapshotting values at write time. Where this feature differs, that
  difference is required rather than chosen, and [plan.md](./plan.md) Complexity Tracking says why
