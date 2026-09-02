# ADR 0002: Decimal columns and aggregation on SQLite

**Status**: Accepted
**Date**: 2026-09-02
**Context feature**: [specs/001-diet-tracking](../../specs/001-diet-tracking/plan.md) (research R-010)

## Context

Every service in this application stores its data in SQLite. EF Core maps `decimal` to SQLite
`TEXT`, because SQLite has no native decimal type. Text columns compare and sort
**lexicographically**, so `SUM`, `AVG`, `ORDER BY` and range comparisons over a decimal column do
not behave numerically. EF Core documents this and will not translate those operations reliably.

The existing `Money` value object stores `decimal` with `HasPrecision(18, 2)`. That has been safe so
far only because nothing aggregates it in SQL — money saved is computed in the domain, in memory,
from a handful of values.

The diet subdomain broke that assumption: average daily calorie intake is an average across days,
computed by the database over potentially years of rows.

## Decision

Two rules for numeric data in this application:

1. **Any value the database will aggregate, order by, or range-compare is stored as an integer**, in
   the smallest sensible whole unit. Calories are `int` kilocalories — nutrition labels are whole
   kilocalories anyway, so nothing is lost.
2. **`decimal` is permitted only for values computed and displayed in memory.** Macronutrient grams
   and body weights stay decimal because they are only ever totalled within one aggregate and never
   aggregated in SQL.

Additionally, `LoggedDay` persists its own daily totals rather than deriving them on read. This
keeps calendar and statistics queries to one small row per day instead of every entry, which is what
meets the sub-second target over a three-year history.

## Consequences

- Averages and sums are exact and translate to SQL cleanly.
- A denormalisation invariant now exists: a day's stored total must equal the sum of its entries. It
  is safe only because ADR 0001 keeps entries and total inside the same aggregate, recomputed
  together on every mutation — and a domain test asserts it directly on every code path.
- Anyone adding a numeric column must decide which of the two rules it falls under **before**
  choosing its type. Discovering it later means a migration.

## Alternatives rejected

- **`double` for calories.** Avoids the TEXT problem but introduces floating-point drift into a
  number members compare against a target for equality.
- **Computing totals from entries on read.** No denormalisation to maintain, but every calendar
  render would load every entry in the period, which fails the performance criterion outright.
- **Moving to a database with a real decimal type.** Out of scope, and the per-service SQLite model
  is a deliberate architectural choice elsewhere.
