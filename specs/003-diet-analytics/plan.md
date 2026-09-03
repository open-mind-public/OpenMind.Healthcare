# Implementation Plan: Diet Analytics

**Branch**: `003-diet-analytics` | **Date**: 2026-09-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-diet-analytics/spec.md`

## Summary

Turn the diet history a member has already built up into answers: where their calories go, whether
their macronutrients match the targets they set, when they eat, and a short list of what the
programme noticed. It reads existing data and adds nothing new to log.

This is additive inside `DietApi`. No new service, no new port, no compose change — and, unusually,
**no migration**: every figure is derived on demand from tables that already exist.

Three decisions shape the construction. Energy is aggregated **in SQL** over the owned
`FoodEntries` table, which a probe confirmed translates cleanly (R-003) — that is what meets the
two-second budget at three years. Macronutrients are aggregated **in memory** from each day's
stored totals, because ADR 0002 forbids aggregating decimals in SQL and a probe showed the
forbidden operation silently returning a *correct* answer on small data, which makes it a trap
rather than a safeguard (R-005). And observations are **declarative rules** with a stated minimum
and threshold each, so "does not fire below its minimum" is a property the test suite can assert
over every rule without knowing what any of them says (R-010).

## Technical Context

**Language/Version**: C# 13 / .NET 10; TypeScript 5.x / Angular 19

**Primary Dependencies**: Unchanged — ASP.NET Core Minimal APIs, MediatR 12.4.1, EF Core 10
(Sqlite + Design), JwtBearer, OpenAPI + Scalar, `Shared/DDD.BuildingBlocks`. No new backend
package and **no new frontend package** (R-013).

**Storage**: The existing `diet.db` and `DietDbContext`, read-only. No migration.

**Testing**: xUnit + Shouldly in the existing `DietApi.Tests`, in-memory fakes from `TestSupport/`.

**Target Platform**: Unchanged — Linux container, non-root, port 5000; browser front end via nginx.

**Project Type**: Additive feature within an existing service and an existing Angular programme.

**Performance Goals**: Every analytics read under 2 seconds at 3 years of daily logging (SC-004).
Each read is a small number of grouped queries; none loads a food entry into memory (R-008).

**Constraints**: Energy is `int` and may be aggregated in SQL; macronutrient grams are `decimal`
and MUST NOT be (ADR 0002, R-005). All instants UTC; calendar days `DateOnly`; every
time-dependent domain method takes `DateTime? asOf = null`. No figure may combine exercise energy
with intake or a target (R-014). Time-of-day analysis needs the caller's UTC offset and is
bucketed at quarter-hour resolution so offsets like +05:30 and +05:45 rotate exactly (R-006).

**Scale/Scope**: 4 user stories, 27 functional requirements, 13 release-gating success criteria
plus 2 post-launch measures. 0 new aggregates, 1 read-model repository, ~8 value objects, 3 domain
services, 7 observation rules, 4 endpoints, 1 new Angular page.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution v1.0.0, ratified 2026-09-01.

### Initial check (before Phase 0)

| Principle | Verdict | How this plan satisfies it |
|--------|--------|--------|
| I. Bounded contexts own their data | PASS | Everything reads `diet.db`, which `DietApi` already owns. No cross-context read, no new foreign key, and `UserId` still arrives only from the JWT via `IUserService`. The one thing this feature might have wanted from elsewhere — a member's timezone — is deliberately taken from the request instead of from `UserApi` (R-006). |
| II. The domain model holds the rules | PASS WITH A DEVIATION | Every judgement — which target applied on a day, what counts as a logged day, what share is worth remarking on, how strong an observation is — lives in `Domain/`. The deviation is that aggregation itself is performed by a read-model repository in SQL rather than by an aggregate method. Justified in Complexity Tracking and recorded as R-009. |
| III. Vertical slices, not layers | PASS | One new `Features/DietAnalytics/` folder following the established shape: one static `DietAnalyticsEndpoints.cs` with `MapGroup("/api/diet-analytics")`, `.WithTags`, `.RequireAuthorization()`, per-route `.WithName().WithOpenApi()`, and one query record plus handler per file. |
| IV. Time is a parameter | PASS | `asOf` flows through period resolution and every "today" comparison. The caller's UTC offset is likewise a parameter, never ambient. |
| V. Domain and slice tests ship with the feature | PASS | Period resolution, each breakdown calculation and every observation rule get domain tests at boundary values; each of the four handlers gets success-path and unauthenticated-path slice tests. Plus two structural tests: no response type combines exercise with intake, and no rule fires below its own minimum. |
| VI. Migrations and idempotent seeds | PASS, VACUOUSLY | No schema change and no reference data, so there is nothing to migrate and nothing to seed. Worth stating rather than leaving as an apparent omission. |

**Architecture & Technology Constraints**: the eight obligations apply to *new services*; this adds
none. No port, volume, database file, compose service or frontend prefix is allocated (R-001).

**Frontend routing**: no new prefix. One new route under the existing `/diet/*` namespace and one
new programme-registry entry, which the shell and left rail consume automatically.

**Result**: PASS with one recorded deviation. Proceed to Phase 0.

### Post-design re-check (after Phase 1)

Re-evaluated against [data-model.md](./data-model.md) and [contracts/](./contracts/).

| Principle | Verdict | Note |
|--------|--------|--------|
| I | PASS | No design artifact introduces a cross-context read or foreign key. The food-library join in R-004 is between two tables the diet context owns. |
| II | PASS WITH THE SAME DEVIATION | Confirmed in the design: the repository returns flat rows carrying no behaviour, and every value object and rule that interprets them is in `Domain/`. |
| III | PASS | The four contract groups map one-to-one onto four handler folders. |
| IV | PASS | Every derived figure takes `asOf`; the period resolver is the only place "today" is read. |
| V | PASS | Test layout enumerated below; quickstart runs it. |
| VI | PASS | Re-confirmed: the design adds no table, column or seed. |

**One new deviation to record**, and it is the read-model repository from R-009 — the first time
this codebase reads its own data through something other than an aggregate repository. It is
justified below rather than hidden, and is proposed as ADR 0004 so the next feature that needs a
reporting read has a precedent to follow instead of a decision to relitigate.

**Result**: PASS. Ready for `/speckit-tasks`.

## Project Structure

### Documentation (this feature)

```text
specs/003-diet-analytics/
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

Additions to the existing service. Nothing existing changes shape, and nothing existing is
modified except the two frontend registration points.

```text
OpenMind.Healthcare/backend/DietApi/
├── Domain/
│   ├── ValueObjects/
│   │   ├── AnalysisPeriod.cs          # NEW - resolved window, comparison window, denominators
│   │   ├── PeriodPreset.cs            # NEW - enum: Week, Month, Quarter, Plan
│   │   ├── IntakeSummary.cs           # NEW - totals, averages, day-state split
│   │   ├── MealBreakdown.cs           # NEW
│   │   ├── FoodContribution.cs        # NEW
│   │   ├── CategoryBreakdown.cs       # NEW
│   │   ├── MacronutrientComparison.cs # NEW - actual vs target, amounts and shares
│   │   ├── WeekdayDistribution.cs     # NEW
│   │   ├── TimeOfDayDistribution.cs   # NEW - 24 hourly buckets, rotated from 96
│   │   └── Observation.cs             # NEW - text, figure, family, strength
│   ├── Observations/                  # NEW
│   │   ├── IObservationRule.cs        # family, minimum days, threshold, Evaluate
│   │   ├── ObservationFamily.cs       # enum - what FR-022 de-duplicates on
│   │   └── Rules/                     # one file per rule, seven of them
│   ├── Services/
│   │   ├── AnalysisPeriodResolver.cs  # NEW - preset + asOf + plan start -> AnalysisPeriod
│   │   ├── IntakeAnalyser.cs          # NEW - rows -> summary, breakdowns, distributions
│   │   └── ObservationEngine.cs       # NEW - runs rules, filters, de-duplicates, orders
│   └── Repositories/
│       └── IDietAnalyticsRepository.cs # NEW - read model; returns rows, not aggregates
├── Features/
│   └── DietAnalytics/                 # NEW - /api/diet-analytics
│       ├── DietAnalyticsEndpoints.cs
│       ├── DietAnalyticsDtos.cs
│       ├── GetIntakeAnalysis/         # US1
│       ├── GetMacroAnalysis/          # US2
│       ├── GetEatingPatterns/         # US3
│       └── GetObservations/           # US4
└── Infrastructure/Data/Repositories/
    └── DietAnalyticsRepository.cs     # NEW - the grouped queries from R-003, R-004, R-006

OpenMind.Healthcare/backend/DietApi.Tests/
├── Domain/                            # NEW - period resolution, each analyser, each rule
├── Features/                          # NEW - slice tests per handler, plus the two structural tests
└── TestSupport/
    └── FakeDietAnalyticsRepository.cs # NEW - in-memory rows

OpenMind.Healthcare/frontend/src/app/
├── components/
│   └── diet-analytics/                # NEW - the Analytics page and its sections
├── programs/programs.ts               # MODIFIED - one nav entry
├── services/diet.service.ts           # MODIFIED - four analytics methods
├── models/diet.models.ts              # MODIFIED - analytics wire types
└── app.module.ts                      # MODIFIED - declarations, one route
```

**Structure Decision**: Additive within `DietApi` and the existing Angular diet programme, per
R-001. The one structural judgement is the read-model repository sitting beside the aggregate
repositories rather than inside them — forced by the shape of the question (R-009), not chosen for
novelty.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| A read-model repository (`IDietAnalyticsRepository`) that returns flat rows instead of aggregate roots, performing aggregation in SQL rather than in the domain (Principle II) | Every question this feature answers is an aggregation across up to ~1,100 days and ~4,400 entries. SQL answers it in one statement per breakdown; the domain cannot, without first loading all of it | **Extending `ILoggedDayRepository`** gives one interface two unrelated jobs — loading an aggregate to mutate it, and summarising thousands of rows — and invites someone to answer a reporting question by loading aggregates. **Loading aggregates and computing in the domain** keeps the pattern pure and fails SC-004 outright at three years, for no benefit a member can see. **Raw SQL** was unnecessary: EF Core translates all of it (R-003, R-004, R-006), so dropping to strings would cost compile-time checking and gain nothing |

The deviation is bounded deliberately: the repository does arithmetic (`SUM`, `COUNT`, `GROUP BY`)
and returns records with no behaviour. Every *judgement* stays in `Domain/` — which target applied
on a given day, what counts as a logged day, which denominator an average used, whether a share is
large enough to remark on, how strong an observation is. That is the half of Principle II worth
protecting, and it stays testable without a database.

Proposed as **ADR 0004** so the next reporting feature inherits a precedent rather than reopening
the argument.

### Inherited, not new

Three entries already in Complexity Tracking from earlier features apply unchanged and are not
re-justified here: the `LoggedDay`/`DietPlan` aggregate split (ADR 0001), integer storage for
anything the database aggregates (ADR 0002), and persisted earned state (ADR 0003).

One thing that looks like a deviation and is not: **food category is read live from the library
rather than snapshotted** (R-004). Everywhere else this codebase snapshots — a food's name, its
nutrition, an activity's MET — so that correcting the catalogue cannot rewrite a member's history.
Category is exempt because it is a classification rather than a figure the member saw and acted on;
reclassifying a food should reclassify it in past periods too. It is the one figure in the feature
that can change for a closed period, and it is correct that it does.
