# ADR 0004: A read model for reporting reads

**Status**: Accepted
**Date**: 2026-09-03
**Context feature**: [specs/003-diet-analytics](../../specs/003-diet-analytics/plan.md) (research R-003, R-005, R-009)

## Context

Every read in this application so far has gone through an aggregate repository: load a
`LoggedDay`, load a `DietPlan`, mutate it, save it. Where a read needed less than the whole
aggregate, the repository projected to a small record — `DaySummary`, `ExerciseDaySummary` — but it
still belonged to the aggregate that owned the rows.

Diet analytics does not fit that shape. It answers questions like *which foods contributed the most
energy over the last ninety days*, *how is intake distributed across the hours of the day*, and
*what share of the period came from each meal*. None of those is a question about one aggregate.
Every one of them is an aggregation across up to about 1,100 logged days and 4,400 food entries.

Answering them through `ILoggedDayRepository` would mean either giving that interface a second,
unrelated job, or loading every aggregate in the period and totalling in memory. The second fails
the feature's one-second criterion outright at three years of history.

## Decision

Reporting reads get their own interface, separate from the aggregate repositories.

`IDietAnalyticsRepository` returns **flat records with no behaviour** — `(meal, kcal, count)`,
`(food, kcal, times)`, `(category, kcal)`, `(date, calories, macros, that day's targets)`,
`(hour, quarter, kcal)`. It performs the arithmetic the database is good at, in SQL, and makes no
judgements at all.

Every judgement stays in `Domain/`: which denominator an average uses, how a day is assessed,
whether a share is large enough to remark on, how strongly an observation applies. Those remain
testable without a database, which is the half of the domain-model principle worth protecting.

The boundary is: **the repository may sum, count and group; it may not decide what anything
means.**

## Consequences

- Analytics reads are one grouped query each, and the whole feature meets its budget at three
  years with the indexes that already exist.
- `ILoggedDayRepository` keeps one job. Nobody is tempted to answer a reporting question by loading
  a thousand aggregates, because the interface for reporting is right there.
- There are now two ways to read data in this codebase, and a reader has to know which is which.
  The rule of thumb: **if the answer is about one member's one thing, use the aggregate repository;
  if it is an aggregation across many, use a read model.**
- The read model is deliberately not generic. It has one method per question the feature asks,
  rather than a query builder, so what the database is asked to do is legible in one file.

## What this does not license

- **It is not a second write path.** Every method is a query, and a test asserts that every method
  on the interface begins with `Get`.
- **It does not bypass the domain.** A handler that took rows from here and computed a member-facing
  judgement inline would be the defect this ADR is trying to prevent.
- **It does not cross a context boundary.** The read model reads only tables the diet context owns.

## Two things the implementation found, worth carrying forward

Both were discovered by running queries against a real SQLite database rather than reasoning about
them, and both would have shipped otherwise.

1. **`SUM` over a `decimal` column returns a plausible answer.** ADR 0002 forbids aggregating
   decimals in SQL because EF Core maps them to `TEXT`. A probe found the forbidden operation
   returning the *correct* total on small clean data — which makes it a trap rather than a
   safeguard, because it will pass every test written over a handful of rows and drift later.
   Macronutrients are therefore summed in memory from per-day rows, which costs nothing at ~1,100
   rows.

2. **Ordering after a `GroupBy` projection does not translate.** Writing
   `.GroupBy(...).Select(r => new Row(...)).OrderByDescending(r => r.Kilocalories).Take(10)` reads
   naturally and throws at runtime; the ordering and the cap must come *before* the projection, on
   the aggregate expression. In-memory fakes sort happily either way, so unit tests cannot catch
   this. The scale test against a real database is what did.

## Alternatives rejected

- **Extend `ILoggedDayRepository`.** One interface, two unrelated jobs — loading an aggregate to
  mutate, and summarising thousands of rows to report. It also puts the reporting queries where the
  next person looking for them would not think to look.
- **Load aggregates and compute in the domain.** Keeps the pattern pure and fails the performance
  criterion at three years, for no benefit a member can perceive.
- **Raw SQL.** Unnecessary: EF Core translates all of these queries, so dropping to strings would
  cost compile-time checking and gain nothing.
- **A stored, incrementally-maintained analytics table.** Would need invalidating on every food
  entry write, and any bug in that invalidation surfaces as analytics that quietly disagree with the
  log they describe. Deriving on demand cannot drift.
