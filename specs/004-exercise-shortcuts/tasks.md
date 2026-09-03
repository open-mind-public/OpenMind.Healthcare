---

description: "Task list for Exercise Shortcuts implementation"
---

# Tasks: Exercise Shortcuts

**Input**: Design documents from `/specs/004-exercise-shortcuts/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Test tasks are **required**, not optional. Constitution Principle V makes domain tests
(every `IBusinessRule` proven to throw, invariants asserted) and slice tests (success and
unauthenticated paths per handler) part of the feature.

**Organization**: Tasks are grouped by user story. Each story is a deployable increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1-US3)
- Paths are repository-relative from `c:\Users\tung.le\Data\git-personal\OpenMind.Healthcare`

## Path Conventions

Additive inside the existing diet service — no new project, port, volume or compose service:

- Backend: `OpenMind.Healthcare/backend/DietApi/`
- Tests: `OpenMind.Healthcare/backend/DietApi.Tests/`
- Front end: `OpenMind.Healthcare/frontend/src/app/`

---

## Phase 1: Setup

- [X] T001 Create the `ExerciseShortcut` owned entity in `OpenMind.Healthcare/backend/DietApi/Domain/Entities/ExerciseShortcut.cs` with `DietPlanId`, `ActivityTypeId`, `Name`, `DurationMinutes`, `Position` and `CreatedAt`. It stores **no** MET, activity name or estimate — a shortcut is an instruction to record in future, so it must not carry figures that will be stale when used (research R-003)
- [X] T002 Create `OpenMind.Healthcare/backend/DietApi/Domain/Rules/ExerciseShortcutRules.cs` implementing `IBusinessRule` for `ShortcutLimitRule` (10), `ShortcutMustBeUniqueRule` (same activity **and** duration), `ShortcutNameMustNotBeEmptyRule`, `ShortcutNameWithinLengthRule` (80) and `ReorderMustCoverEveryShortcutRule`, each naming itself via `nameof`. Do **not** add duration rules — reuse `DurationMustBePositiveRule` and `DurationWithinCeilingRule` from 002, which is what makes FR-005 true rather than merely intended

**Checkpoint**: Shared types exist; the service still builds unchanged.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The aggregate, its persistence and the migration. Every story writes through these.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 Add the shortcut collection to `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/DietPlan.cs`: a private backing field, `IReadOnlyCollection<ExerciseShortcut> ExerciseShortcuts`, `MaxShortcuts = 10`, `RemainingShortcutSlots`, `ExerciseShortcut(id)` and `ShortcutsInOrder()`. Owned rather than its own aggregate because the cap and the no-duplicates rule are invariants over the whole set (research R-002) — put that reason in the remarks
- [X] T004 Add `SaveExerciseShortcut`, `RenameExerciseShortcut`, `ReorderExerciseShortcuts` and `RemoveExerciseShortcut` to `OpenMind.Healthcare/backend/DietApi/Domain/Aggregates/DietPlan.cs`. Every one normalises positions to `0..n-1` afterwards, so a removal leaves no hole and no caller can create two shortcuts at the same position
- [X] T005 Configure the owned collection in `OpenMind.Healthcare/backend/DietApi/Infrastructure/Data/DietDbContext.cs`: `OwnsMany` with `ToTable("ExerciseShortcuts")`, `WithOwner().HasForeignKey(s => s.DietPlanId)`, `HasKey(s => s.Id)`, `ValueGeneratedNever()`, `Name` max length 80, a **unique** index on `(DietPlanId, ActivityTypeId, DurationMinutes)` backing FR-006 at the storage layer, a supporting index on `(DietPlanId, Position)`, `Ignore(s => s.DomainEvents)`, and `UsePropertyAccessMode(PropertyAccessMode.Field)` on the navigation
- [X] T006 Generate the migration with `dotnet ef migrations add AddExerciseShortcuts -o Infrastructure/Data/Migrations` from `OpenMind.Healthcare/backend/DietApi`, verify it applies to an empty database, and confirm the unique index actually appears in the generated SQL rather than assuming it survived the owned-entity mapping (research R-011)
- [X] T007 [P] Extend `OpenMind.Healthcare/backend/DietApi.Tests/TestSupport/DietPlanBuilder.cs` with a fluent `WithShortcut(activity, minutes, name)` so tests can build a plan that already has shortcuts
- [X] T008 [P] Add shortcut wire types to `OpenMind.Healthcare/frontend/src/app/models/diet.models.ts` for every shape in [contracts/rest-api.md](./contracts/rest-api.md), including `available` and `remainingSlots`

**Checkpoint**: The schema exists and the aggregate enforces its rules. No user-visible change yet.

---

## Phase 3: User Story 1 - Record a repeated session in one tap (Priority: P1) 🎯 MVP

**Goal**: A member saves a session as a shortcut and records the same session again with one tap.

**Independent Test**: Record a 45 minute run, save it as a shortcut, tap it on another day, and
confirm the recorded session is indistinguishable from one entered by hand.

**Depends on**: Phase 2.

### Domain tests for User Story 1

- [X] T009 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/ExerciseShortcutRulesTests.cs` proving each rule throws when broken and passes at its boundary: the tenth shortcut saves and the eleventh does not, an identical activity-and-duration pair is refused while a different duration is allowed, a blank name is refused, and an 80-character name is allowed
- [X] T010 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/ExerciseShortcutOrderingTests.cs` asserting the position invariant directly — after every save, rename, reorder and removal, positions are exactly `0..n-1` with no gaps and no duplicates
- [X] T011 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Domain/ShortcutDurationRulesTests.cs` proving a shortcut refuses exactly the durations a session refuses, by exercising the same boundary values 002 uses (0, 1, 1440, 1441). This is FR-005 asserted rather than assumed

### Endpoints for User Story 1

- [X] T012 [US1] Create `OpenMind.Healthcare/backend/DietApi/Features/ExerciseShortcuts/ExerciseShortcutDtos.cs` with the shortcut shape and the list response from [contracts/rest-api.md](./contracts/rest-api.md). `activityName` is resolved from the catalogue on read and `available` is false when the activity is gone — neither is stored
- [X] T013 [US1] Create `GetShortcutsHandler` in `OpenMind.Healthcare/backend/DietApi/Features/ExerciseShortcuts/GetShortcuts/GetShortcutsHandler.cs`, resolving each shortcut's activity name from the catalogue and reporting `remainingSlots` so a client can say how many more may be added before the limit
- [X] T014 [US1] Create `CreateShortcutHandler` in `OpenMind.Healthcare/backend/DietApi/Features/ExerciseShortcuts/CreateShortcut/CreateShortcutHandler.cs`, deriving a readable default name from the activity and duration when none is given, and returning the full list
- [X] T015 [US1] Create `OpenMind.Healthcare/backend/DietApi/Features/ExerciseShortcuts/ExerciseShortcutsEndpoints.cs` on `/api/exercise-shortcuts` with `.RequireAuthorization()` and the get and create routes, translating `DomainException` to 400 and a missing plan to 404
- [X] T016 [US1] Create `AddEntryFromShortcutHandler` in `OpenMind.Healthcare/backend/DietApi/Features/Exercise/AddEntryFromShortcut/AddEntryFromShortcutHandler.cs`, resolving the shortcut then performing exactly what the by-hand path performs — same rules, same `ExerciseDay.AddEntry`, estimate from `plan.CurrentWeightKg()` at this moment. Both paths must end in the same aggregate method (research R-005)
- [X] T017 [US1] Add the `POST /{date}/entries/from-shortcut` route to `OpenMind.Healthcare/backend/DietApi/Features/Exercise/ExerciseEndpoints.cs`, translating a stale `version` to 409 and an unknown or foreign shortcut to 404
- [X] T018 [US1] Register the shortcut endpoints in `OpenMind.Healthcare/backend/DietApi/Program.cs`
- [X] T019 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/ExerciseShortcutHandlerTests.cs` covering get and create: success, unauthenticated, no plan, an unknown activity, a duplicate naming the existing shortcut, the limit, and another member's shortcuts being unreachable
- [X] T020 [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/RecordedByShortcutMatchesByHandTests.cs` — **the task not to skip**. Record the same session both ways and compare the resulting entries field by field: activity, snapshotted name, snapshotted MET, duration and estimate. Then prove the estimate moves with the member's weight: save a shortcut, change the weight, tap again, and assert the new session's estimate differs while the first is byte-for-byte unchanged (FR-010, SC-002, SC-003)
- [X] T021 [P] [US1] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/ShortcutRecordingRulesTests.cs` proving the tap is refused for a future date, a pre-plan date and a stale day version, with the same reasons a typed session gives (FR-012)

### Front end for User Story 1

- [X] T022 [US1] Add `getShortcuts`, `createShortcut` and `addExerciseEntryFromShortcut` to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`
- [X] T023 [US1] Add a shortcut row above the search box in `OpenMind.Healthcare/frontend/src/app/components/exercise-log/` (ts, html, css): one button per shortcut, tapping records, and the whole row is disabled with the reason when the day in view cannot accept a session (FR-013). Use design tokens only — no raw colour values, and no new dependency
- [X] T024 [US1] Add a **save as a shortcut** action to each recorded session in `OpenMind.Healthcare/frontend/src/app/components/exercise-log/exercise-log.component.html`, so saving is one interaction from where the session is shown (FR-001, SC-004), with the duplicate and limit messages surfaced in place

**Checkpoint**: User Story 1 is complete. Quickstart V1, V2 and V3 pass. **V2 is the one that
matters.**

---

## Phase 4: User Story 2 - Keep the list worth having (Priority: P2)

**Goal**: A member renames, reorders and removes shortcuts, and the list stays theirs.

**Independent Test**: With three shortcuts, rename one, move another to the front, delete the third,
reload, and confirm all three changes survived and no recorded session changed.

**Depends on**: US1 — there must be shortcuts to curate.

- [X] T025 [US2] Create `RenameShortcutHandler` in `OpenMind.Healthcare/backend/DietApi/Features/ExerciseShortcuts/RenameShortcut/RenameShortcutHandler.cs`, returning the full list and 404 when the shortcut is not the caller's
- [X] T026 [US2] Create `ReorderShortcutsHandler` in `OpenMind.Healthcare/backend/DietApi/Features/ExerciseShortcuts/ReorderShortcuts/ReorderShortcutsHandler.cs`, taking the **complete** ordered list of ids and refusing a list that is not exactly the member's current shortcuts. A full-list reorder is idempotent and race-free where move-up and move-down are not (research R-004)
- [X] T027 [US2] Create `DeleteShortcutHandler` in `OpenMind.Healthcare/backend/DietApi/Features/ExerciseShortcuts/DeleteShortcut/DeleteShortcutHandler.cs`, re-normalising positions so no hole is left
- [X] T028 [US2] Add the rename, reorder and delete routes to `OpenMind.Healthcare/backend/DietApi/Features/ExerciseShortcuts/ExerciseShortcutsEndpoints.cs`
- [X] T029 [P] [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/ShortcutCurationHandlerTests.cs` covering rename, reorder and delete: success, unauthenticated, no plan, a shortcut that is not the caller's, a reorder missing an id, and a reorder containing an id the member does not own
- [X] T030 [US2] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/DeletingAShortcutLeavesSessionsAloneTests.cs` proving that renaming, reordering and deleting a shortcut leave every session recorded from it byte-for-byte unchanged — a shortcut is a button, not a record (FR-017, SC-009)
- [X] T031 [US2] Add `renameShortcut`, `reorderShortcuts` and `deleteShortcut` to `OpenMind.Healthcare/frontend/src/app/services/diet.service.ts`
- [X] T032 [US2] Add a manage view to `OpenMind.Healthcare/frontend/src/app/components/exercise-log/` with rename, move up, move down and remove. Reordering sends the full ordered list; move controls rather than drag-and-drop, which would be the front end's only new dependency for a list of ten (research R-009)

**Checkpoint**: Quickstart V4 passes.

---

## Phase 5: User Story 3 - Build one without logging it first (Priority: P3)

**Goal**: A member creates a shortcut by choosing an activity and a duration directly.

**Independent Test**: Create a shortcut for an activity never logged, tap it, and confirm it records
correctly.

**Depends on**: US1 for the create endpoint. Independent of US2.

- [X] T033 [US3] Add a create-from-scratch form to `OpenMind.Healthcare/frontend/src/app/components/exercise-log/` reusing the existing activity type-ahead and a duration field, calling the same create endpoint US1 built
- [X] T034 [P] [US3] Extend `OpenMind.Healthcare/backend/DietApi.Tests/Features/ExerciseShortcutHandlerTests.cs` with creation for an activity that has never been logged, and with durations of 0 and above the ceiling refused with the same wording a session gives (FR-002, FR-005, SC-005)

**Checkpoint**: All three user stories are functional. Quickstart V5 passes.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T035 [P] Write `OpenMind.Healthcare/backend/DietApi.Tests/Features/ShortcutBoundaryTests.cs` asserting structurally that no shortcut response type carries an estimate, a MET or a stored activity name, and that no `/api/exercise` response for a recorded session carries a shortcut field — a session records what happened, not which button produced it (FR-010)
- [X] T036 [P] Extend `OpenMind.Healthcare/backend/DietApi.Tests/Features/DayVerdictUnchangedTests.cs` so the guarantee is proven for the **shortcut** recording path too. Adding a second way to record is exactly how a guarantee tested only on the first path gets lost (FR-019)
- [X] T037 [P] Update the `DietApi` row of the route-group table in `README.md` to add `/api/exercise-shortcuts`
- [X] T038 Work through every scenario in [quickstart.md](./quickstart.md) V1 to V6 and record the results, **V2 especially**
- [X] T039 Run the constitution's gates: `dotnet build OpenMind.Healthcare.sln` with no new warnings, `dotnet test OpenMind.Healthcare.sln` passing, `npm run build` in `OpenMind.Healthcare/frontend/` succeeding **with no new dependency in `package.json`**, the new endpoints appearing in Scalar and requiring authorization, and the migration applying to an empty database

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup — **blocks every user story**
- **US1 (Phase 3)**: Depends on Foundational
- **US2 (Phase 4)**: Depends on US1 — there must be shortcuts to curate
- **US3 (Phase 5)**: Depends on US1's create endpoint. Independent of US2
- **Polish (Phase 6)**: Depends on every story

### Story Dependency Graph

```text
Setup → Foundational → US1 ─┬─→ US2
                            └─→ US3
```

US1 is the trunk, as in 002: it builds both the create endpoint and the recording path that the
other two extend. US3 is small because US1 already built its endpoint — it is a front-end form over
work already done.

### Parallel Opportunities

- T007 and T008 in Foundational run together
- Within US1: T009, T010, T011 together; then T019 and T021 together
- Across stories: once US1 lands, US2 and US3 can be built at once — they share
  `ExerciseShortcutsEndpoints.cs`, `diet.service.ts` and the exercise-log component, so those touch
  points are sequential and everything else is disjoint
- In Polish: T035, T036, T037 together

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup — T001 to T002
2. Phase 2 Foundational — T003 to T008 (**blocks everything**)
3. Phase 3 User Story 1 — T009 to T024
4. **STOP and VALIDATE**: run quickstart V1, V2 and V3
5. Deploy or demo — a member records their daily run with one tap

The MVP is 24 tasks and delivers exactly what was asked for.

### Incremental Delivery

1. Setup + Foundational → the aggregate enforces its rules and the schema exists
2. + US1 → **MVP**: save a shortcut, tap it, record
3. + US2 → a list that stays worth having
4. + US3 → a shortcut for something not yet done
5. + Polish → guarantees gated, README current, quickstart green

---

## Notes

- `[P]` marks tasks touching different files with no dependency on incomplete work
- `[Story]` labels map each task to a user story; Setup, Foundational and Polish carry none by design
- Test tasks are required, not optional — Principle V makes domain and slice tests part of the
  feature
- **T020 is the task not to skip.** The whole design rests on a shortcut holding no estimate, and
  that test is what proves a saved button does not freeze the member's weight at the moment they
  saved it
- **T002 deliberately adds no duration rules.** Reusing 002's rule objects is what makes "a duration
  refused on a session is refused on a shortcut" true by construction rather than by coincidence
- Commit after each task or logical group; stop at any checkpoint to validate a story independently
