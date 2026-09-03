---

description: "Task list for Diet Analytics implementation"
---

# Tasks: Diet Analytics

**Input**: Design documents from `/specs/003-diet-analytics/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Test tasks are **required**, not optional. Constitution Principle V makes domain tests
(calculations covered at boundary values, every rule proven) and slice tests (success path and
unauthenticated path per handler) part of the feature.

**Organization**: Tasks are grouped by user story. Each story is a deployable increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1-US4)
- Paths are repository-relative from `c:\Users\tung.le\Data\git-personal\OpenMind.Healthcare`

## Path Conventions

Additive inside the existing diet service — no new project, port, volume, compose service **or
migration**:

- Backend: `OpenMind.Healthcare/backend/DietApi/`
- Tests: `OpenMind.Healthcare/backend/DietApi.Tests/`
- Front end: `OpenMind.Healthcare/frontend/src/app/`

> **If a task seems to need `dotnet ef migrations add`, stop.** This feature reads existing data and
> changes no model. A migration means something was added that should not have been (research
> R-002), and the quickstart's definition of done asserts its absence.

---

## Phase 1: Setup

**Purpose**: The primitives every later type references.

- [X] T001 [P] Create `PeriodPreset` enum in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/PeriodPreset.cs` with `Week`, `Month`, `Quarter`, `Plan`, and a comment recording the day counts each resolves to (7, 30, 90, whole plan)
- [X] T002 [P] Create `ObservationFamily` enum in `OpenMind.Healthcare/backend/DietApi/Domain/Observations/ObservationFamily.cs` with `Timing`, `Composition`, `Targets`, `Consistency`. This is what FR-022 de-duplicates on, so the comment should say so
- [X] T003 Create the `Observation` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/Observation.cs` with `Family`, `Text`, `Figure`, `Strength` and `BasedOnDays`. `Figure` is carried separately from `Text` so a test can assert the number and the screen can emphasise it (FR-017)

**Checkpoint**: Shared types exist; the service still builds unchanged.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Period resolution and the read model. Every user story computes against a resolved
period and reads through this repository, so nothing can start until both exist.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Create `OpenMind.Healthcare/backend/DietApi/Domain/Rules/AnalysisPeriodRules.cs` implementing `IBusinessRule` for `PeriodMustFallWithinPlanRule` and `PeriodMustNotBeEmptyRule`, each naming itself via `nameof` and taking the comparison dates as parameters rather than reading the clock
- [X] T005 Create the `AnalysisPeriod` value object in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/AnalysisPeriod.cs` holding `Preset`, `From`, `To`, `WasNarrowed`, `TotalDays`, `LoggedDays`, `PreviousFrom`, `PreviousTo` and `HasComparison`. `HasComparison` is false rather than the previous window being zeros — zeros would assert the member did nothing in a period that does not exist (research R-012)
- [X] T006 Create `AnalysisPeriodResolver` in `OpenMind.Healthcare/backend/DietApi/Domain/Services/AnalysisPeriodResolver.cs` turning a preset, the plan start date and `asOf` into an `AnalysisPeriod`, clamping to `[planStart, today]` and setting `WasNarrowed` when clamping changed the requested window (FR-002). Pure — no clock of its own, no repository
- [X] T007 Create `IDietAnalyticsRepository` in `OpenMind.Healthcare/backend/DietApi/Domain/Repositories/IDietAnalyticsRepository.cs`, declaring the five flat row records (`DayIntakeRow`, `MealIntakeRow`, `FoodContributionRow`, `CategoryIntakeRow`, `QuarterHourRow`) alongside the interface, following the way `DaySummary` sits beside `ILoggedDayRepository`. These rows carry no behaviour by design (research R-009)
- [X] T008 Create `DietAnalyticsRepository` in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Repositories/DietAnalyticsRepository.cs` with `GetDayRowsAsync` returning one `DayIntakeRow` per logged day in range, projecting each day's stored totals **and its own stored target snapshot** — that snapshot is what makes FR-011 possible. Every query filters by `UserId` so another member's data is unreachable rather than merely forbidden
- [X] T009 [P] Create `OpenMind.Healthcare/backend/DietApi.Tests/TestSupport/FakeDietAnalyticsRepository.cs` as an in-memory `IDietAnalyticsRepository` built from a list of seeded days, with fluent helpers for a day's intake, its target and its entries
- [X] T010 [P] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/AnalysisPeriodResolverTests.cs` covering each preset, a plan shorter than the requested window (`WasNarrowed` true), a window with no room before it (`HasComparison` false), the whole-plan preset having no comparison at all, and a plan starting today
- [X] T011 Create `OpenMind.Healthcare/backend/DietApi/Features/DietAnalytics/DietAnalyticsDtos.cs` with the shared `period` block and the four response records from [contracts/rest-api.md](./contracts/rest-api.md). Every average travels in the same object as its denominator so a client cannot show one without the other (FR-003)
- [X] T012 Create `OpenMind.Healthcare/backend/DietApi/Features/DietAnalytics/DietAnalyticsEndpoints.cs` with `MapGroup("/api/diet-analytics")`, `.WithTags("DietAnalytics")` and `.RequireAuthorization()`, and no routes yet. Every route added later is a `GET`; this feature has no verb that writes
- [X] T013 Register `IDietAnalyticsRepository`, `AnalysisPeriodResolver` and `MapDietAnalyticsEndpoints()` in `OpenMind.Healthcare/backend/DietApi/Program.cs`
- [X] T014 [P] Add analytics wire types to `OpenMind.Healthcare/frontend/src/app/models/diet.models.ts` for every response shape in [contracts/rest-api.md](./contracts/rest-api.md), including the shared period block and the `averagedOver` denominator field

**Checkpoint**: A period resolves correctly and day rows can be read. No user-visible change yet.

---

## Phase 3: User Story 1 - See where my calories actually go (Priority: P1) 🎯 MVP

**Goal**: A member sees their intake broken down by meal, by food and by category over a chosen
period, with every average carrying the number of days behind it.

**Independent Test**: Log a month of varied meals, open analytics, and confirm the meal figures sum
to the reported total, the category figures sum to the same total, and the day-state counts sum to
the calendar days in the period rather than the logged ones.

**Depends on**: Phase 2.

### Domain for User Story 1

- [X] T015 [P] [US1] Create `IntakeSummary` in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/IntakeSummary.cs` with total and average energy, the `AveragedOverDays` denominator, the `AveragedOver` label, and the on-target/over-target/not-logged split. The intake average uses **logged** days and the day-state split uses **all** days — two denominators in one type, deliberately, because either alone would mislead (research R-011)
- [X] T016 [P] [US1] Create `MealBreakdown` in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/MealBreakdown.cs` and `CategoryBreakdown` in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/CategoryBreakdown.cs`, each exhaustive over its enum so meals and categories with nothing logged appear at zero and the parts always sum to the total (FR-006)
- [X] T017 [P] [US1] Create `FoodContribution` in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/FoodContribution.cs` with the food, its energy, its share and how many times it was logged. Deliberately **not** exhaustive — a top ten, and the type must not imply its shares sum to 100
- [X] T018 [US1] Create `IntakeAnalyser` in `OpenMind.Healthcare/backend/DietApi/Domain/Services/IntakeAnalyser.cs` turning repository rows into the summary and the three breakdowns, computing shares as percentages with the **largest remainder** absorbing rounding drift so displayed parts still sum to 100 (SC-002)

### Read model for User Story 1

- [X] T019 [US1] Add `GetMealRowsAsync`, `GetTopFoodRowsAsync` and `GetCategoryRowsAsync` to `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Repositories/DietAnalyticsRepository.cs`, each a single grouped query over `SelectMany(d => d.Entries)` — a probe confirmed this translates to SQL with a join rather than loading entries (research R-003). Categories come from a join to `FoodLibraryItems`, because `FoodEntry` does not snapshot one (research R-004)

### Tests for User Story 1

- [X] T020 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/IntakeAnalyserTests.cs` asserting the reconciliation invariants directly: meal energies sum to the total, category energies sum to the same total, and displayed shares sum to 100 after rounding
- [X] T021 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/IntakeDenominatorTests.cs` proving the intake average divides by logged days, the day-state split sums to the period's calendar days, and a member who logged three days of thirty gets an average over three with the denominator carried on the figure (FR-003, SC-003)

### Endpoint for User Story 1

- [X] T022 [US1] Create `GetIntakeAnalysisHandler` in `OpenMind.Healthcare/backend/DietApi/Features/DietAnalytics/GetIntakeAnalysis/GetIntakeAnalysisHandler.cs`, returning null when the member has no plan so the endpoint can answer 404, and returning a populated response with zeros and an empty breakdown for a member with a plan and no logged days
- [X] T023 [US1] Add the `GET /intake` route to `OpenMind.Healthcare/backend/DietApi/Features/DietAnalytics/DietAnalyticsEndpoints.cs`, translating `DomainException` to 400 and a missing plan to 404
- [X] T024 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/IntakeAnalysisHandlerTests.cs` covering the success path, the unauthenticated path, a member with no plan, a period with no logged days, and another member's days being excluded

### Front end for User Story 1

- [X] T025 [US1] Add `getIntakeAnalysis` to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`
- [X] T026 [US1] Create the analytics page shell at `OpenMind.Healthcare/frontend/src/app/components/diet-analytics/` (ts, html, css) with the period selector, the shared period line stating what was actually analysed when `wasNarrowed` is true, and a plain empty state for a member with nothing logged (SC-005). Use design tokens only — no raw colour values, and **no charting dependency** (research R-013)
- [X] T027 [US1] Add the intake section to `OpenMind.Healthcare/frontend/src/app/components/diet-analytics/diet-analytics.component.html` and `.css`: totals, the meal and category breakdowns as CSS bars, the top-food list, and the day-state split. Every average must render its denominator beside it
- [X] T028 [US1] Register the programme nav entry `{ path: 'analytics', label: 'Analytics', icon: 'trending' }` in the diet programme in `OpenMind.Healthcare/frontend/src/app/programs/programs.ts`, add the `diet/analytics` route guarded by `AuthGuard` and declare `DietAnalyticsComponent` in `OpenMind.Healthcare/frontend/src/app/app.module.ts`

**Checkpoint**: User Story 1 is complete and quickstart V1 passes. A member can see where their
calories went.

---

## Phase 4: User Story 2 - See whether my eating matches my targets (Priority: P2)

**Goal**: A member sees protein, carbohydrate and fat against the targets that were in force,
across the period.

**Independent Test**: Log days whose macronutrient split is far from target, then confirm the
reported comparison matches a hand calculation — including across a period where the target changed.

**Depends on**: Phase 2. Independent of US1.

- [X] T029 [P] [US2] Create `MacronutrientComparison` in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/MacronutrientComparison.cs` with actual and target grams, the share of energy for each, `HasTargets`, and the `AveragedOverDays` denominator
- [X] T030 [US2] Create `MacronutrientAnalyser` in `OpenMind.Healthcare/backend/DietApi/Domain/Services/MacronutrientAnalyser.cs` summing grams **in memory** across the day rows, never in SQL. A probe showed `SUM` over the decimal column silently returning a correct answer on small data, which makes it a trap rather than a safeguard — ADR 0002 stands (research R-005). Energy shares use 4/4/9 kcal per gram
- [X] T031 [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/MacronutrientAnalyserTests.cs` — **including the task not to skip in this story**: a period spanning a target change must compare against the average of each day's own stored target, not the plan's current one, verified against a hand calculation (FR-011, SC-006). Also cover a plan with no macronutrient targets, where the split appears and nothing is compared
- [X] T032 [US2] Create `GetMacroAnalysisHandler` in `OpenMind.Healthcare/backend/DietApi/Features/DietAnalytics/GetMacroAnalysis/GetMacroAnalysisHandler.cs`, returning null for a member with no plan
- [X] T033 [US2] Add the `GET /macros` route to `OpenMind.Healthcare/backend/DietApi/Features/DietAnalytics/DietAnalyticsEndpoints.cs`
- [X] T034 [P] [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/MacroAnalysisHandlerTests.cs` covering success, unauthenticated, no plan, no macronutrient targets, and an empty period
- [X] T035 [US2] Add `getMacroAnalysis` to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`
- [X] T036 [US2] Add the macronutrient section to `OpenMind.Healthcare/frontend/src/app/components/diet-analytics/diet-analytics.component.html` and `.css`, showing actual against target as paired bars, and rendering the split alone — with no comparison and no substituted plan target — when `hasTargets` is false (FR-012)

**Checkpoint**: Quickstart V2 passes, step 2 especially.

---

## Phase 5: User Story 3 - See the pattern in when I eat (Priority: P3)

**Goal**: A member sees intake distributed across the days of the week and the hours of their own
day.

**Independent Test**: Log a fortnight with deliberately heavy weekends and late evenings, then
confirm the reported distribution matches the seeded pattern — and that it shifts correctly for a
half-hour timezone offset.

**Depends on**: Phase 2. Independent of US1 and US2.

- [X] T037 [P] [US3] Create `WeekdayDistribution` in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/WeekdayDistribution.cs` with seven entries, each carrying its average energy and how many days of that weekday were logged. Derived from each day's `DateOnly` calendar date, which has no timezone component at all
- [X] T038 [P] [US3] Create `TimeOfDayDistribution` in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/TimeOfDayDistribution.cs` with 24 hourly entries, the `UtcOffsetMinutes` applied, and `IsApproximate` with its reason — the time recorded is when the entry was logged, not when the food was eaten (FR-015)
- [X] T039 [US3] Create `PatternAnalyser` in `OpenMind.Healthcare/backend/DietApi/Domain/Services/PatternAnalyser.cs` building both distributions, rotating the 96 quarter-hour buckets by the caller's offset. Quarter-hour resolution is what makes +05:30 and +05:45 land exactly instead of approximately (research R-006)
- [X] T040 [US3] Add `GetQuarterHourRowsAsync` to `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/Repositories/DietAnalyticsRepository.cs`, grouping by hour and `minute / 15` — a probe confirmed this translates (research R-006)
- [X] T041 [US3] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/PatternAnalyserTests.cs` — **the task not to skip in this story**: prove the rotation is exact at `+05:30`, `+05:45`, `-03:30` and a negative whole-hour offset, and that energy is conserved across the rotation. Also cover a weekday with nothing logged reading as zero with zero logged days rather than being absent
- [X] T042 [US3] Create `GetEatingPatternsHandler` in `OpenMind.Healthcare/backend/DietApi/Features/DietAnalytics/GetEatingPatterns/GetEatingPatternsHandler.cs`, taking the caller's UTC offset as a parameter and defaulting it to zero
- [X] T043 [US3] Add the `GET /patterns` route to `OpenMind.Healthcare/backend/DietApi/Features/DietAnalytics/DietAnalyticsEndpoints.cs`, accepting `utcOffsetMinutes` from the query string
- [X] T044 [P] [US3] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/EatingPatternsHandlerTests.cs` covering success, unauthenticated, no plan, a missing offset defaulting to zero, and an implausible offset being rejected rather than silently applied
- [X] T045 [US3] Add `getEatingPatterns` to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`, sending the browser's offset via `new Date().getTimezoneOffset()` — negated, because the browser reports the opposite sign to the one the contract expects
- [X] T046 [US3] Add the patterns section to `OpenMind.Healthcare/frontend/src/app/components/diet-analytics/diet-analytics.component.html` and `.css` as a seven-column weekday chart and a 24-hour histogram in CSS, with the approximation stated on the screen and not only in the payload (FR-015)

**Checkpoint**: Quickstart V3 passes, step 3 especially.

---

## Phase 6: User Story 4 - Be told what the numbers say (Priority: P4)

**Goal**: A short, ordered list of what the programme noticed, each with its figure, and silence
when nothing meets its threshold.

**Independent Test**: Seed a member with a deliberate pattern and confirm the matching observation
appears with the right figure; seed a member with nine logged days and confirm nothing fires.

**Depends on**: US1, US2 and US3 — every rule reads figures those stories produce.

- [X] T047 [US4] Create `AnalyticsFigures` in `OpenMind.Healthcare/backend/DietApi/Domain/ValueObjects/AnalyticsFigures.cs` as the composite a rule is evaluated against: the period, the intake summary, the breakdowns, the macronutrient comparison and the two distributions
- [X] T048 [US4] Create `IObservationRule` in `OpenMind.Healthcare/backend/DietApi/Domain/Observations/IObservationRule.cs` declaring `Family`, `MinimumLoggedDays`, `ThresholdDescription` and `Evaluate(AnalyticsFigures) -> Observation?`. The minimum is declared data rather than buried in the evaluation, so a single test can assert it over every rule without knowing what any rule says (FR-018)
- [X] T049 [P] [US4] Create the timing rules in `OpenMind.Healthcare/backend/DietApi/Domain/Observations/Rules/TimingRules.cs`: `LateEatingRule` (≥ 25% of energy after 21:00 local, minimum 14 logged days) and `WeekendHeavierRule` (weekend daily average ≥ 20% above weekday, minimum 14 days with at least 2 weekend and 4 weekday days)
- [X] T050 [P] [US4] Create the composition rules in `OpenMind.Healthcare/backend/DietApi/Domain/Observations/Rules/CompositionRules.cs`: `SingleFoodDominanceRule` (one food ≥ 15% of energy), `MealSkewRule` (one meal ≥ 45%) and `LowPlantShareRule` (fruit + vegetables < 10%), each with a minimum of 14 logged days
- [X] T051 [P] [US4] Create the target and consistency rules in `OpenMind.Healthcare/backend/DietApi/Domain/Observations/Rules/TargetAndConsistencyRules.cs`: `ProteinBelowTargetRule` (average protein ≤ 80% of average target, requires the plan to have macronutrient targets) and `LoggingImprovedRule` (logged days up ≥ 25% on the previous window, minimum 14 days in both)
- [X] T052 [US4] Create `ObservationEngine` in `OpenMind.Healthcare/backend/DietApi/Domain/Services/ObservationEngine.cs` running every rule, skipping those whose minimum exceeds the period's logged days, discarding nulls, keeping only the strongest per family (FR-022), and ordering by strength with a stable tie-break on family. Pure — no clock, no repository, no randomness, which is how FR-020 becomes a property of the arithmetic rather than a matter of discipline
- [X] T053 [US4] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/ObservationRuleTests.cs` covering each of the seven rules at its threshold and one unit either side, and proving the wording carries the figure it rests on (FR-017)
- [X] T054 [US4] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/ObservationEngineTests.cs` — **the task not to skip in this story**. Enumerate every registered rule and assert generically that none fires below its own `MinimumLoggedDays` (SC-008); assert the same figures produce an identical, identically-ordered list twice (SC-009, FR-020); assert two rules of one family yield only the stronger (FR-022); assert an empty result when nothing meets a threshold (FR-021)
- [X] T055 [US4] Create `GetObservationsHandler` in `OpenMind.Healthcare/backend/DietApi/Features/DietAnalytics/GetObservations/GetObservationsHandler.cs`, assembling `AnalyticsFigures` and returning `nothingStoodOut` plus `minimumDaysForAnyObservation` so a member with too little history is told why they see nothing rather than shown a blank
- [X] T056 [US4] Add the `GET /observations` route to `OpenMind.Healthcare/backend/DietApi/Features/DietAnalytics/DietAnalyticsEndpoints.cs` and register `ObservationEngine` and every rule in `OpenMind.Healthcare/backend/DietApi/Program.cs`
- [X] T057 [P] [US4] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/ObservationsHandlerTests.cs` covering success, unauthenticated, no plan, a member below every minimum, and a member whose data triggers several families
- [X] T058 [US4] Add `getObservations` to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`
- [X] T059 [US4] Add the observations section to the top of `OpenMind.Healthcare/frontend/src/app/components/diet-analytics/diet-analytics.component.html` and `.css`, emphasising each `figure` within its sentence, and rendering the "nothing stood out" and "keep logging, {n} days needed" states as plain sentences rather than empty panels

**Checkpoint**: All four user stories are functional. Quickstart V4 passes.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T060 [P] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/AnalyticsBoundaryTests.cs` asserting structurally that no analytics response type carries a field combining exercise energy with intake or a target — no `net`, no `available`, no `burned` offsetting `consumed`. This is the guarantee 002 was shaped around, restated where it is most tempting to break (FR-023, SC-007)
- [X] T061 [P] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/AnalyticsAreReadOnlyTests.cs` proving that running every analytics handler leaves each logged day's stored target, consumed total and assessment unchanged, and that the plan is untouched (FR-024, SC-011)
- [X] T062 Extend `OpenMind.Healthcare/backend/DietApi.Tests/Infrastructure/ThreeYearHistoryTests.cs` with each of the four analytics reads over three years of daily logging, timing each against the budget and asserting no query materialises food entries (SC-004). If it fails, the query shapes in research R-008 are what to revisit — not the criterion
- [X] T063 Confirm the index usage carried over from research R-008's open question: capture the query plan for the intake read at three-year scale and record in `OpenMind.Healthcare/backend/DietApi.Tests/Infrastructure/ThreeYearHistoryTests.cs` that it uses the existing `LoggedDays(UserId, Date)` index rather than scanning
- [X] T064 Review every observation the system can produce against FR-019 line by line — none may diagnose a condition, call the member's eating good or bad, or instruct them what to eat — and record the reviewed wording in `OpenMind.Healthcare/backend/DietApi/Domain/Observations/Rules/` as a comment on each rule (SC-010)
- [X] T065 [P] Write ADR 0004 at `OpenMind.Healthcare/adrs/0004-read-model-for-reporting-reads.md` recording the read-model repository decision, what was tried instead and why it was rejected, so the next reporting feature inherits a precedent rather than reopening the argument (plan Complexity Tracking, research R-009)
- [X] T066 [P] Update the `DietApi` row of the route-group table in `README.md` to add `/api/diet-analytics`, and add the diet programme's analytics capability to the Programmes table
- [X] T067 Work through every scenario in [quickstart.md](./quickstart.md) V1 to V6 and record the results, **V2 step 2 and V4 steps 3 to 5 especially**
- [X] T068 Run the constitution's gates: `dotnet build OpenMind.Healthcare.sln` with no new warnings, `dotnet test OpenMind.Healthcare.sln` passing, `npm run build` in `OpenMind.Healthcare/frontend/` succeeding **with no new dependency in `package.json`**, the four endpoints appearing in Scalar and requiring authorization, and **no migration having been added**

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — **blocks every user story**
- **US1 (Phase 3)**: Depends on Foundational. No dependency on another story
- **US2 (Phase 4)**: Depends on Foundational. Independent of US1
- **US3 (Phase 5)**: Depends on Foundational. Independent of US1 and US2
- **US4 (Phase 6)**: Depends on US1, US2 and US3 — every rule reads figures they produce
- **Polish (Phase 7)**: Depends on every story being shipped

### Story Dependency Graph

```text
Setup → Foundational ─┬─→ US1 ─┐
                      ├─→ US2 ─┼─→ US4
                      └─→ US3 ─┘
```

The shape is the inverse of 002's. There the first story was the trunk and the rest were branches;
here the first three are genuinely parallel — each reads the same day rows and computes something
different — and the fourth is the join, because an observation is a statement about figures the
others produce.

### Within Each User Story

- Value objects before the domain service that builds them
- Domain service before the handler that calls it
- Repository query beside the story that needs it, not all at once up front
- Domain tests alongside the domain, before handlers
- Handlers before endpoints, endpoints before the front end

### Parallel Opportunities

- All of Phase 1 runs together except T003, which needs T002's enum
- T009, T010 and T014 in Foundational run together
- Within US1: T015, T016, T017 together; then T020, T021 together
- Within US4: T049, T050, T051 — the three rule files — together
- **Across stories**: once Foundational lands, US1, US2 and US3 can be built by three people at
  once. They share `DietAnalyticsEndpoints.cs`, `diet.service.ts` and the analytics component, so
  those touch points are sequential; everything else is disjoint
- In Polish: T060, T061, T065, T066 together

---

## Parallel Example: after Foundational

```bash
# Three stories, three developers, no shared domain code:
Developer A: T015-T028  (US1 - where the calories go)
Developer B: T029-T036  (US2 - macronutrients against targets)
Developer C: T037-T046  (US3 - weekday and time-of-day patterns)

# Then whoever is free:
Anyone:      T047-T059  (US4 - observations, which needs all three)
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup — T001 to T003
2. Phase 2 Foundational — T004 to T014 (**blocks everything**)
3. Phase 3 User Story 1 — T015 to T028
4. **STOP and VALIDATE**: run quickstart V1
5. Deploy or demo — a member can see where their calories went

The MVP is 28 tasks. It answers the first question anyone asks of their own food log, and the spec
says plainly that without it the rest is decoration.

### Incremental Delivery

1. Setup + Foundational → a period resolves and day rows read
2. + US1 → **MVP**: where the calories went
3. + US2 → whether the eating matched the targets
4. + US3 → when the eating happened
5. + US4 → what the programme noticed
6. + Polish → guarantees gated, ADR written, quickstart green

---

## Notes

- `[P]` marks tasks touching different files with no dependency on incomplete work
- `[Story]` labels map each task to a user story; Setup, Foundational and Polish carry none by design
- Test tasks are required, not optional — Principle V makes domain and slice tests part of the
  feature, and the constitution's gates check them
- **Three tasks not to skip**, one per risk this feature carries: **T031** proves a period spanning
  a target change is judged against each day's own stored target; **T041** proves the time-of-day
  rotation is exact at half-hour offsets; **T054** proves no observation ever fires below its own
  minimum and that the same data always yields the same list
- **No migration.** If one appears necessary, something was added to the model that this feature
  was not supposed to add (research R-002)
- **No new dependency**, backend or frontend. Charts are CSS and inline SVG, following the
  calendar's precedent (research R-013)
- Commit after each task or logical group; stop at any checkpoint to validate a story independently
