# Implementation Plan: Exercise Logging

**Branch**: `002-exercise-logging` | **Date**: 2026-09-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-exercise-logging/spec.md`

## Summary

Add exercise logging to the existing diet programme: a member records what they did and for how
long against a calendar date, chosen from a curated activity catalogue, and sees an estimate of the
energy used. The estimate is shown and nothing else moves — the day's calorie target and its
on-target verdict are untouched, and the plan's declared activity level is unaffected.

This is additive inside `DietApi`. No new service, no new ports, no compose changes.

Two decisions shape the construction. Exercise days are their **own aggregate** rather than part of
`LoggedDay`, because `LoggedDay` is created lazily and deleted when its last meal is removed — so
owning exercise there would make a run vanish when a member deleted their dinner, which FR-013
forbids (R-002). And the energy estimate is **snapshotted onto the entry as an integer**, reusing
both the food library's protection against retroactive rewriting and ADR 0002's rule that anything
the database aggregates must be an integer (R-004).

## Technical Context

**Language/Version**: C# 13 / .NET 10; TypeScript 5.x / Angular 19

**Primary Dependencies**: Unchanged — ASP.NET Core Minimal APIs, MediatR 12.4.1, EF Core 10
(Sqlite + Design), JwtBearer, OpenAPI + Scalar, `Shared/DDD.BuildingBlocks`. No new packages.

**Storage**: The existing `diet.db` and `DietDbContext`. One new migration, `AddExerciseLogging`,
adding three tables.

**Testing**: xUnit + Shouldly in the existing `DietApi.Tests`, in-memory fakes from `TestSupport/`.

**Target Platform**: Unchanged — Linux container, non-root, port 5000; browser front end via nginx.

**Project Type**: Additive feature within an existing service and an existing Angular programme.

**Performance Goals**: Calendar and summary under 1 second at 3 years of daily history (SC-004);
activity search under 1 second (SC-003); day totals update with no manual refresh (SC-002).

**Constraints**: Kilocalories and minutes as `int` (ADR 0002 — `decimal` maps to SQLite `TEXT` and
cannot be aggregated). MET values as `decimal`, never SQL-aggregated. All instants UTC, calendar
days as `DateOnly`, every time-dependent domain method takes `DateTime? asOf = null`. Exercise must
never alter a `LoggedDay`, its target snapshot or its assessment.

**Scale/Scope**: 4 user stories, 30 functional requirements, 11 release-gating success criteria plus
2 post-launch measures. 1 new aggregate plus 1 reference aggregate, ~10 endpoints, a 60–80 activity
seed, 1 new Angular page and 2 modified ones.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution v1.0.0, ratified 2026-09-01.

### Initial check (before Phase 0)

| Principle | Verdict | How this plan satisfies it |
|--------|--------|--------|
| I. Bounded contexts own their data | PASS | Everything lands inside `DietApi`, which already owns `diet.db`. No new cross-context read: the member's weight comes from `DietPlan` in the same context, and `UserId` still arrives only from the JWT via `IUserService` (FR-029). |
| II. The domain model holds the rules | PASS | The estimate, the per-day totals and every validation live in `Domain/` — an `ExerciseDay` aggregate, `IBusinessRule` implementations in `Domain/Rules/`, and an `EnergyEstimator` domain service. Handlers resolve the member, load one aggregate, call one method, persist, map. |
| III. Vertical slices, not layers | PASS | New `Features/Exercise/` and `Features/ActivityCatalogue/` folders following the established shape: one static `<Feature>Endpoints.cs` per group with `MapGroup("/api/<kebab-case>")`, `.WithTags`, `.RequireAuthorization()`, per-route `.WithName().WithOpenApi()`, and one command/query record plus handler per file. |
| IV. Time is a parameter | PASS | `asOf` flows through date validation, the weekly summary window and every "today" comparison. Instants UTC, calendar concepts `DateOnly`. |
| V. Domain and slice tests ship with the feature | PASS | The new aggregate gets domain tests proving each rule throws and covering the estimate at boundary values; every command handler gets success-path and unauthenticated-path slice tests. Existing `DietApi.Tests`, already in the solution. |
| VI. Migrations and idempotent seeds | PASS | One checked-in migration. The activity catalogue seeds through the existing guarded `DbInitializer` pattern (`if (!context.X.Any())`, single `SaveChanges`). `OwnsMany` with explicit column names and `Ignore(e => e.DomainEvents)` throughout. |

**Architecture & Technology Constraints**: the eight items apply to *new services*; this adds none,
so they are satisfied by the host service unchanged. No port, volume, database file, compose service
or frontend prefix is allocated — see R-001.

**Frontend routing**: no new prefix. One new route under the existing `/diet/*` namespace and one
new programme-registry entry, which the shell and left rail consume automatically.

**Result**: PASS, no violations. Proceed to Phase 0.

### Post-design re-check (after Phase 1)

Re-evaluated against [data-model.md](./data-model.md) and [contracts/](./contracts/).

| Principle | Verdict | Note |
|--------|--------|--------|
| I | PASS | No design artifact introduces a cross-context read or foreign key. |
| II | PASS | All 6 business rules and the estimate sit inside the domain; contract errors are domain exceptions surfacing. |
| III | PASS | Contract groups map one-to-one onto the feature folders in the source tree below. |
| IV | PASS | Every derived value — daily totals, weekly summary, date validation — takes `asOf`. |
| V | PASS | Test layout enumerated below; quickstart runs it. |
| VI | PASS | Seed set and idempotency guard specified in the data model. |

**No new deviations to record.** This feature introduces no departure from the reference
implementation: it follows the `LoggedDay` shape it was modelled on, and where it differs — a
lifecycle independent of food — that difference is required by FR-013 rather than chosen. The three
entries already in Complexity Tracking from 001 (aggregate split, stored per-day totals, persisted
earned state) are inherited unchanged.

**Result**: PASS. Ready for `/speckit-tasks`.

## Project Structure

### Documentation (this feature)

```text
specs/002-exercise-logging/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── rest-api.md      # Phase 1 output
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created here)
```

### Source Code (repository root)

Additions to the existing service; nothing existing changes shape.

```text
OpenMind.Healthcare/backend/DietApi/
├── Domain/
│   ├── Aggregates/
│   │   ├── ExerciseDay.cs            # NEW - own aggregate, owns ExerciseEntry
│   │   └── ActivityType.cs           # NEW - seeded reference data, carries MET
│   ├── Entities/
│   │   └── ExerciseEntry.cs          # NEW - owned by ExerciseDay
│   ├── ValueObjects/
│   │   ├── ActivityCategory.cs       # NEW - enum
│   │   ├── ExerciseTotals.cs         # NEW - minutes + kilocalories, both int
│   │   └── ActivitySummary.cs        # NEW - active days, total time, trend
│   ├── Rules/
│   │   └── ExerciseEntryRules.cs     # NEW - date and duration rules
│   ├── Services/
│   │   └── EnergyEstimator.cs        # NEW - MET x weight x hours
│   ├── Events/
│   │   └── ExerciseEvents.cs         # NEW
│   └── Repositories/
│       ├── IExerciseDayRepository.cs # NEW
│       └── IActivityTypeRepository.cs# NEW
├── Features/
│   ├── Exercise/                     # NEW - /api/exercise
│   │   ├── ExerciseEndpoints.cs
│   │   ├── ExerciseDtos.cs
│   │   ├── GetExerciseDay/
│   │   ├── GetExerciseRange/
│   │   ├── GetActivitySummary/
│   │   ├── AddExerciseEntry/
│   │   ├── UpdateExerciseEntry/
│   │   └── DeleteExerciseEntry/
│   └── ActivityCatalogue/            # NEW - /api/activity-catalogue
│       ├── ActivityCatalogueEndpoints.cs
│       ├── ActivityCatalogueDtos.cs
│       └── SearchActivities/
└── Infrastructure/Data/
    ├── DietDbContext.cs              # MODIFIED - 2 DbSets + configuration
    ├── DbInitializer.cs              # MODIFIED - one more guarded seed
    ├── Seeds/ActivityCatalogueSeed.cs# NEW - 60-80 activities with MET values
    ├── Migrations/                   # NEW - AddExerciseLogging
    └── Repositories/
        ├── ExerciseDayRepository.cs  # NEW
        └── ActivityTypeRepository.cs # NEW

OpenMind.Healthcare/backend/DietApi.Tests/
├── Domain/                           # NEW - rules, totals invariant, estimator, summary
├── Features/                         # NEW - slice tests incl. the day-verdict guarantee
└── TestSupport/                      # NEW - fakes and an ExerciseDayBuilder

OpenMind.Healthcare/frontend/src/app/
├── components/
│   ├── exercise-log/                 # NEW - the add/edit control
│   ├── activity-summary/             # NEW - the Activity page (US4)
│   ├── diet-dashboard/               # MODIFIED - exercise section on Today
│   └── diet-calendar/                # MODIFIED - exercise marking
├── programs/programs.ts              # MODIFIED - one nav entry
├── services/diet.service.ts          # MODIFIED - exercise methods
├── models/diet.models.ts             # MODIFIED - exercise types
└── app.module.ts                     # MODIFIED - 2 declarations, 1 route
```

**Structure Decision**: Additive within `DietApi` and the existing Angular diet programme, per
R-001. The one structural judgement is that `ExerciseDay` is a sibling of `LoggedDay` rather than a
part of it — forced by their differing lifecycles (R-002), not chosen for symmetry.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified.

No violations, and no new deviations from the reference implementation. The design reuses three
patterns already established and justified in 001 — a per-day aggregate with stored totals, a
`Guid` concurrency token, and snapshotting values captured at write time — rather than introducing
anything that needs defending.

The one thing worth recording for a reader who expects otherwise: **exercise is deliberately not
part of `LoggedDay`**, even though both are per-day collections of member entries under a plan.
`LoggedDay` is destroyed when its last food entry is deleted, so an exercise entry living there
would be destroyed with it. That is not a stylistic split; it is FR-013 made structural.
