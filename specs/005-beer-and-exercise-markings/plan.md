# Implementation Plan: Beer Days and Calendar Activity Markings

**Branch**: `main` (no feature branch — no `before_plan` hook configured) | **Date**: 2026-09-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-beer-and-exercise-markings/spec.md`

## Summary

Let a member mark a date as a beer day, give beer days and exercise days each a dedicated on-cell
marking on the diet calendar (without hiding the eating-state colour), and add a **Habits** section
to diet analytics showing beer and exercise frequency and how eating outcomes on beer days compare
with other days.

This is additive inside `DietApi` and the existing Angular diet programme. One new aggregate
(`BeerDay`), one migration (`AddBeerDays`, one table), three write/read endpoints under
`/api/beer-days`, one new analytics read (`/api/diet-analytics/habits`), one pure domain service
(`HabitAnalyser`). No new service, port, database file, or compose change.

The shape follows 002 (exercise logging) almost exactly: a per-day aggregate that is a sibling of
`LoggedDay`, created and destroyed by the member's action, fetched as its own calendar range and
merged client-side. It is simpler than `ExerciseDay` in two ways — no child entries and no
concurrency token — because a beer day has no mutable state, only existence (research.md R-001, R-002).

## Technical Context

**Language/Version**: C# 13 / .NET 10; TypeScript 5.x / Angular 19

**Primary Dependencies**: Unchanged — ASP.NET Core Minimal APIs, MediatR 12, EF Core 10 (SQLite),
JwtBearer, OpenAPI + Scalar, `Shared/DDD.BuildingBlocks`. No new packages.

**Storage**: The existing `diet.db` / `DietDbContext`. One new migration, `AddBeerDays`, adding one
table (`BeerDays`) with a unique `(DietPlanId, Date)` index and a `(UserId, Date)` range index.

**Testing**: xUnit + Shouldly in the existing `DietApi.Tests`, in-memory fakes in `TestSupport/`.

**Target Platform**: Unchanged — Linux container, port 5000; browser front end via nginx.

**Project Type**: Additive feature within an existing service and an existing Angular programme.

**Performance Goals**: Calendar renders in under 1 second at 3 years of history (the beer range is one
small integer-free row set, lighter than the eating range). Marking round-trips in well under the
10-second budget of SC-001.

**Constraints**: All instants UTC; calendar days `DateOnly`; every time-dependent domain path takes
`DateTime? asOf = null` / `DateOnly today`. Beer marking must never load or alter a `LoggedDay`, a
target, a streak, or any average (FR-010). Component CSS references colour tokens only — the two new
marking colours are added to `styles.scss` (research.md R-004).

**Scale/Scope**: 3 user stories, 17 functional requirements, 6 success criteria. 1 new aggregate,
2 new domain rules, 1 domain service, 2 new analytics value objects, 4 endpoints, 1 migration,
~4 new backend test files. Frontend: `diet-calendar` (component + template + CSS), `diet-analytics`
(component + template + CSS), `diet.service.ts`, `diet.models.ts`, 2 tokens in `styles.scss`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution v1.0.0, ratified 2026-09-01.

### Initial check (before Phase 0)

| Principle | Verdict | How this plan satisfies it |
|--------|--------|--------|
| I. Bounded contexts own their data | PASS | Everything lands in `DietApi` / `diet.db`. `BeerDay` references `DietPlan` by `Guid` only — no navigation, no cross-aggregate FK. `UserId` still arrives only from the JWT via `IUserService` (FR-016). |
| II. The domain model holds the rules | PASS | The two date rules are `IBusinessRule` types in `Domain/Rules/`; every analytics figure and the beer-vs-non-beer comparison live in `HabitAnalyser` and its value objects, tested without a database. Handlers resolve the member, load/save one aggregate (or gather three read sets) and map. |
| III. Vertical slices, not layers | PASS | New `Features/BeerDays/` folder in the established shape: one static `BeerDaysEndpoints.cs` with `MapGroup("/api/beer-days")`, `.WithTags`, `.RequireAuthorization()`, per-route `.WithName().WithOpenApi()`; one command/query record + handler per file. The analytics read is one more file under the existing `Features/DietAnalytics/`. |
| IV. Time is a parameter | PASS | `BeerDay.Mark` takes `asOf`; `HabitAnalyser` takes `today` and `planStart`; the period comes from the existing `AnalysisPeriodResolver`. Instants UTC, calendar days `DateOnly`. |
| V. Domain and slice tests ship with the feature | PASS | `BeerDay` gets domain tests proving each rule throws; `HabitAnalyser` gets boundary tests; every handler gets a success-path and an unauthenticated-path slice test with in-memory fakes. |
| VI. Migrations and idempotent seeds | PASS | One checked-in `AddBeerDays` migration. No seed — a beer day is member data, not reference data — so there is nothing to make idempotent. `Ignore(e => e.DomainEvents)` on the new entity. |

**Architecture & Technology Constraints**: the eight new-service items do not apply — this adds no
service. No port, volume, database file, compose service, or frontend path prefix is allocated
(research.md R-007).

**Frontend routing**: no new prefix and no new route. The beer toggle lives in a popover on the
existing `/diet/calendar`; the analytics section is inside the existing `/diet/analytics`.

**Result**: PASS, no violations. Proceed to Phase 0.

### Post-design re-check (after Phase 1)

Re-evaluated against [data-model.md](./data-model.md) and [contracts/rest-api.md](./contracts/rest-api.md).

| Principle | Verdict | Note |
|--------|--------|--------|
| I | PASS | No design artifact introduces a cross-context read or an FK. `HabitAnalyser` reads three of `DietApi`'s own repositories. |
| II | PASS | `Mark` is the only state transition and it is rule-guarded; `HabitAnalyser` is pure and holds the whole comparison. |
| III | PASS | Contract groups map one-to-one onto `Features/BeerDays/` and one file under `Features/DietAnalytics/`. |
| IV | PASS | `asOf` / `today` / `planStart` flow through every date decision; nothing reads an ambient clock. |
| V | PASS | Test list enumerated in [quickstart.md](./quickstart.md); the fakes follow the existing `TestSupport/` pattern. |
| VI | PASS | One migration, no seed. |

**No new deviations to record.** This feature inherits the three entries already in 001/002
Complexity Tracking (per-day aggregate, a read model that is not an aggregate repository, snapshotting
— though this feature snapshots nothing). It removes ceremony rather than adding it: no child table,
no concurrency token.

**Result**: PASS. Ready for `/speckit-tasks`.

## Project Structure

### Documentation (this feature)

```text
specs/005-beer-and-exercise-markings/
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

Additions to the existing service and programme; nothing existing changes shape.

```text
OpenMind.Healthcare/backend/DietApi/
├── Domain/
│   ├── Aggregates/
│   │   └── BeerDay.cs                    # NEW - aggregate root, no children, no version
│   ├── Rules/
│   │   └── BeerDayRules.cs               # NEW - future-date and pre-plan-start rules
│   ├── Events/
│   │   └── BeerEvents.cs                 # NEW - BeerDayMarkedEvent
│   ├── Repositories/
│   │   └── IBeerDayRepository.cs         # NEW
│   ├── Services/
│   │   └── HabitAnalyser.cs              # NEW - pure; beer/exercise frequency + eating split
│   └── ValueObjects/
│       ├── EatingOutcome.cs             # NEW - on/over/not-logged split for a day group
│       └── HabitAnalysis.cs             # NEW - the analyser's result
├── Features/
│   ├── BeerDays/                         # NEW - /api/beer-days
│   │   ├── BeerDaysEndpoints.cs
│   │   ├── BeerDaysDtos.cs
│   │   ├── MarkBeerDay/MarkBeerDayHandler.cs
│   │   ├── UnmarkBeerDay/UnmarkBeerDayHandler.cs
│   │   └── GetBeerDayRange/GetBeerDayRangeHandler.cs
│   └── DietAnalytics/
│       ├── DietAnalyticsDtos.cs          # MODIFIED - HabitInsightsResponse + mapper
│       ├── DietAnalyticsEndpoints.cs     # MODIFIED - GET /habits
│       └── GetHabitInsights/GetHabitInsightsHandler.cs   # NEW
└── Infrastructure/Data/
    ├── DietDbContext.cs                  # MODIFIED - DbSet<BeerDay> + configuration
    ├── Migrations/                       # NEW - AddBeerDays
    └── Repositories/
        └── BeerDayRepository.cs          # NEW

OpenMind.Healthcare/backend/DietApi.Tests/
├── Domain/
│   ├── BeerDayRulesTests.cs              # NEW
│   └── HabitAnalyserTests.cs            # NEW
├── Features/
│   ├── BeerDayHandlerTests.cs            # NEW
│   └── HabitInsightsHandlerTests.cs     # NEW
└── TestSupport/
    ├── FakeBeerDayRepository.cs          # NEW
    └── BeerDayBuilder.cs                 # NEW

OpenMind.Healthcare/frontend/src/
├── styles.scss                          # MODIFIED - --beer-mark, --exercise-mark (light + dark)
└── app/
    ├── components/
    │   ├── diet-calendar/               # MODIFIED - beer range fetch + merge, day popover,
    │   │                                #            beer + exercise indicators, legend, .css
    │   └── diet-analytics/              # MODIFIED - Habits section (component + .html + .css)
    ├── services/diet.service.ts         # MODIFIED - beer + habits methods
    └── models/diet.models.ts            # MODIFIED - BeerDayRange, HabitInsights, EatingOutcome
```

**Program.cs** — MODIFIED: register `IBeerDayRepository` → `BeerDayRepository`, `HabitAnalyser`,
and `app.MapBeerDaysEndpoints()`.

**Structure Decision**: Additive within `DietApi` and the existing Angular diet programme
(research.md R-007). `BeerDay` is a sibling of `LoggedDay` and `ExerciseDay` — forced by lifecycle
(a beer day must survive the day's food being cleared, and exist on a food-free day) exactly as
`ExerciseDay` was in 002.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified.

No violations. The design is a strict simplification of the `ExerciseDay` pattern already in the
codebase: same sibling-aggregate lifecycle, minus the owned entry collection and minus the
concurrency token, because a beer day carries no state that a stale write could corrupt
(research.md R-002).

The one thing worth recording for a reader who expects otherwise: **a beer day is deliberately not a
flag on `LoggedDay`**, even though it is a yes/no fact about a date. `LoggedDay` is destroyed when its
last food entry is removed and does not exist on a food-free day, so a flag there would lose beer
days both times. That is FR-004 and the food-free-beer-day edge case made structural, not a
stylistic split.
