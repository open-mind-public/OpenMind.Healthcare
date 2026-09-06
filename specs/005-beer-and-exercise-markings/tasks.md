---
description: "Task list for Beer Days and Calendar Activity Markings"
---

# Tasks: Beer Days and Calendar Activity Markings

**Input**: Design documents from `/specs/005-beer-and-exercise-markings/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/rest-api.md

**Tests**: Included — Constitution Principle V makes domain and slice tests part of the feature.

**Organization**: Grouped by user story. Paths are repo-relative.
`be/` = `OpenMind.Healthcare/backend/DietApi`, `bet/` = `OpenMind.Healthcare/backend/DietApi.Tests`,
`fe/` = `OpenMind.Healthcare/frontend/src`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: different file, no dependency on another unstarted task — safe to parallelise
- **[Story]**: US1 (mark a beer day) · US2 (tell markings apart) · US3 (analytics)

---

## Phase 1: Setup

- [X] T001 Confirm `dotnet ef` is available and `DietApi` builds clean (`dotnet build OpenMind.Healthcare.sln`).

---

## Phase 2: Foundational (blocks every story)

**Purpose**: the `BeerDay` aggregate, its store, and DI wiring — nothing in US1/US2/US3 works without it.

- [X] T002 [P] Create `BeerDayRules` in `be/Domain/Rules/BeerDayRules.cs` — `BeerDateCannotBeInFutureRule`, `BeerDateCannotPrecedePlanStartRule` (mirror `ExerciseEntryRules`).
- [X] T003 [P] Create `BeerDayMarkedEvent` in `be/Domain/Events/BeerEvents.cs`.
- [X] T004 Create `BeerDay` aggregate in `be/Domain/Aggregates/BeerDay.cs` — `DietPlanId`, `UserId`, `Date`; private ctor; `static Mark(dietPlanId, userId, date, planStartDate, asOf = null)` checking both rules and emitting the event. No `Version`, no entries. (depends on T002, T003)
- [X] T005 [P] Create `IBeerDayRepository` in `be/Domain/Repositories/IBeerDayRepository.cs` — `GetByDateAsync`, `GetDatesInRangeAsync(userId, from, to) -> IReadOnlyList<DateOnly>`, `AddAsync`, `DeleteAsync`.
- [X] T006 Implement `BeerDayRepository` in `be/Infrastructure/Data/Repositories/BeerDayRepository.cs`. (depends on T005)
- [X] T007 Add `DbSet<BeerDay> BeerDays` and its `modelBuilder.Entity<BeerDay>` configuration (keys, `(DietPlanId, Date)` unique index, `(UserId, Date)` index, `Ignore(DomainEvents)`) in `be/Infrastructure/Data/DietDbContext.cs`. (depends on T004)
- [X] T008 Generate migration `AddBeerDays` (`dotnet ef migrations add AddBeerDays`) and confirm it is one `CREATE TABLE` + two indexes; check the model snapshot updated. (depends on T007)
- [X] T009 Register `IBeerDayRepository -> BeerDayRepository` and `HabitAnalyser` in `be/Program.cs`, and add `app.MapBeerDaysEndpoints()`. (endpoint mapping compiles after T012)
- [X] T010 [P] Create `FakeBeerDayRepository` in `bet/TestSupport/FakeBeerDayRepository.cs` (`Empty()`, `Containing(...)`, `SaveCount`, `DeleteCount`) and `BeerDayBuilder` in `bet/TestSupport/BeerDayBuilder.cs`.
- [X] T011 [P] Domain tests in `bet/Domain/BeerDayRulesTests.cs` — future date throws, pre-plan-start throws, a valid past date builds.

**Checkpoint**: aggregate, store, migration, fakes in place.

---

## Phase 3: User Story 1 — Mark a day as a beer day (P1) 🎯 MVP

**Goal**: a member marks/unmarks a beer day from the calendar and it persists.

**Independent test**: mark several past dates, reload the calendar, see the beer indicator; remove one, see it clear.

### Backend

- [X] T012 [US1] Create `Features/BeerDays/BeerDaysEndpoints.cs` + `BeerDaysDtos.cs` — `MapGroup("/api/beer-days")`, `.WithTags("BeerDays")`, `.RequireAuthorization()`; routes `GET /`, `PUT /{date}`, `DELETE /{date}` with `.WithName().WithOpenApi()`; `DomainException -> 400`, null plan `-> 404`. DTOs: `BeerDayRangeResponse(from, to, IReadOnlyList<DateOnly> days)`, `BeerDayResponse(date, isBeerDay)`.
- [X] T013 [P] [US1] `Features/BeerDays/MarkBeerDay/MarkBeerDayHandler.cs` — resolve member, load plan (null → null result → 404), `GetByDateAsync`; if absent `BeerDay.Mark(...)` + `AddAsync`; if present, no-op (idempotent). Returns `BeerDayResponse`.
- [X] T014 [P] [US1] `Features/BeerDays/UnmarkBeerDay/UnmarkBeerDayHandler.cs` — resolve member, load plan, `GetByDateAsync`; if present `DeleteAsync`; if absent, no-op. Returns a found/notfound-agnostic success.
- [X] T015 [US1] `Features/BeerDays/GetBeerDayRange/GetBeerDayRangeHandler.cs` — resolve member, load plan, `GetDatesInRangeAsync`, filter to `>= plan.StartDate && <= today`, return `BeerDayRangeResponse`. (mirrors `GetExerciseRangeHandler`)
- [X] T016 [US1] Slice tests in `bet/Features/BeerDayHandlerTests.cs` — mark, mark-again idempotent (`SaveCount` unchanged the second time), unmark, unmark-when-absent, future date → `DomainException`, no plan → null, another member's date invisible, range excludes pre-plan and future, anonymous → `UnauthorizedAccessException`.

### Frontend

- [X] T017 [P] [US1] Add wire types to `fe/app/models/diet.models.ts` — `BeerDayRange { from; to; days: string[] }`.
- [X] T018 [US1] Add `getBeerRange(from, to)`, `markBeerDay(date)`, `unmarkBeerDay(date)` to `fe/app/services/diet.service.ts`.
- [X] T019 [US1] `fe/app/components/diet-calendar/diet-calendar.component.ts` — fetch the beer range in the `forkJoin` (degrade to empty on error, like exercise), build `beerByDate: Set<string>`, add `isBeer(day)`; add a `selectedDay` popover with a "🍺 Beer day" toggle calling `markBeerDay`/`unmarkBeerDay` and updating the set, plus an "Open food log" button that calls the existing `openDay`. Cell click opens the popover for within-plan days instead of navigating directly.
- [X] T020 [US1] `diet-calendar.component.html` — render the popover/modal; keep the grid button but route its click to the popover.

**Checkpoint**: US1 works end to end — marking persists and shows on the calendar.

---

## Phase 4: User Story 2 — Tell beer days and exercise days apart (P2)

**Goal**: beer and exercise each have a dedicated on-cell marking, distinct from each other and from the eating colours, in both views, explained in the legend, and not colour-only.

**Independent test**: a month with every combination — each day's facts readable from the calendar alone, including year view.

- [X] T021 [P] [US2] Add `--beer-mark` and `--exercise-mark` tokens (light `:root` + both dark blocks) to `fe/styles.scss`.
- [X] T022 [US2] `diet-calendar.component.css` — move `.exercise-dot` fill to `var(--exercise-mark)` (keep the `--surface` ring); add `.beer-mark` as a bottom-right bar in `var(--beer-mark)`; add year-view scaled variants; add `.swatch.beer` / `.swatch.exercise` legend variants. (depends on T021)
- [X] T023 [US2] `diet-calendar.component.html` — add the beer indicator span on the cell (shape/position distinct from the exercise dot); add a **Beer day** legend item; keep both indicators visible alongside the eating fill.
- [ ] T024 [US2] Manual check per quickstart step 2 and 5 (over-target + exercise + beer; year view) — screenshot. *(not yet run — needs the stack up)*

**Checkpoint**: US1 + US2 both work.

---

## Phase 5: User Story 3 — Beer and exercise in analytics (P3)

**Goal**: a Habits section shows beer/exercise frequency and a beer-day vs non-beer-day eating comparison for the selected period.

**Independent test**: seed a known spread, open analytics, confirm counts and the comparison match; change the period; zero beer days shows zero.

### Backend

- [X] T025 [P] [US3] Create `EatingOutcome` in `be/Domain/ValueObjects/EatingOutcome.cs` — `From(onTarget, overTarget, notLogged)`; zero-day group is all zeros.
- [X] T026 [US3] Create `HabitAnalysis` in `be/Domain/ValueObjects/HabitAnalysis.cs`. (depends on T025)
- [X] T027 [US3] Create `HabitAnalyser` in `be/Domain/Services/HabitAnalyser.cs` — `Analyse(period, planStart, today, loggedDays, beerDates, exerciseDates)`; derive in-plan days and each day's state; intersect beer/exercise dates with the in-plan set; per-week rates over `InPlanDays`. (depends on T026)
- [X] T028 [P] [US3] Domain tests in `bet/Domain/HabitAnalyserTests.cs` — counts, per-week rate, beer vs non-beer split summing correctly, zero beer days, sub-week period, a beer date before plan start ignored.
- [X] T029 [US3] Add `HabitInsightsResponse` + `EatingOutcomeDto` + mapper to `be/Features/DietAnalytics/DietAnalyticsDtos.cs`.
- [X] T030 [US3] Create `be/Features/DietAnalytics/GetHabitInsights/GetHabitInsightsHandler.cs` — deps `IDietPlanRepository`, `IDietAnalyticsRepository`, `IBeerDayRepository`, `IExerciseDayRepository`, `AnalysisPeriodResolver`, `HabitAnalyser`, `IUserService`; resolve period, gather logged rows + beer dates + exercise summary dates, call analyser, map. Null plan → null. (depends on T027, T029)
- [X] T031 [US3] Add `GET /habits` to `be/Features/DietAnalytics/DietAnalyticsEndpoints.cs` (`period` query, default `Month`, `DomainException -> 400`, null → 404). (depends on T030)
- [X] T032 [US3] Slice tests in `bet/Features/HabitInsightsHandlerTests.cs` — success-path figures, no plan → null, anonymous → throws, period follows the preset.

### Frontend

- [X] T033 [P] [US3] Add `HabitInsights` + `EatingOutcome` types to `fe/app/models/diet.models.ts`.
- [X] T034 [US3] Add `getHabitInsights(period)` to `fe/app/services/diet.service.ts`.
- [X] T035 [US3] `fe/app/components/diet-analytics/diet-analytics.component.ts` — `habits` field, `loadHabits()` in `load()`, getters for the comparison bars and the zero-beer case.
- [X] T036 [US3] `diet-analytics.component.html` + `.css` — a **Habits** section: beer days + per-week, exercise days + per-week, and the beer-day vs non-beer-day eating-outcome comparison, reusing the existing bar style; zero-state copy when there are no beer days.

**Checkpoint**: all three stories functional.

---

## Phase 6: Polish & validation

- [X] T037 `dotnet build OpenMind.Healthcare.sln` — 0 errors; `dotnet test` — 805 passed (614 DietApi + 191 QuitSmoking), 0 failed. (New endpoint file adds 3 `WithOpenApi` ASPDEPR002 warnings, matching every existing endpoint file in the codebase.)
- [X] T038 `npm run build` in `fe/` — clean (pre-existing bundle-budget warning only).
- [ ] T039 Run the `quickstart.md` manual walkthrough; confirm new endpoints show in Scalar and 401 without a token. *(not yet run — needs the stack up; the `AddBeerDays` migration is created but not applied to the running container)*
- [X] T040 Re-check Constitution Check in `plan.md` — still holds; one guard test (`AnalyticsBoundaryTests`) was refined to permit an exercise/beer *frequency* count while still forbidding any energy figure attached to them (005 FR-012 vs 003 FR-023).

---

## Dependencies & Execution Order

- **Phase 2** blocks everything. Within it: T002/T003 → T004 → T007 → T008; T005 → T006; T010/T011 parallel.
- **US1 (Phase 3)** after Phase 2. Backend T012–T016; frontend T017–T020. T013/T014 parallel after T012.
- **US2 (Phase 4)** after US1's calendar changes (T019/T020) since it edits the same files; T021 parallel anytime.
- **US3 (Phase 5)** after Phase 2. Independent of US1/US2 except it reuses `IBeerDayRepository` (T005/T006) and `--*-mark` tokens are not needed. Backend T025→T026→T027→T028; T029→T030→T031→T032. Frontend T033–T036.
- **Phase 6** last.

## Implementation Strategy

MVP = Phase 2 + Phase 3 (US1): a member can mark beer days and see them on the calendar. Ship, then
add US2 (clearer markings) and US3 (analytics) as increments.
