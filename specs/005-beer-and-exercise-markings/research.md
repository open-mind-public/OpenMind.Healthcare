# Phase 0 Research: Beer Days and Calendar Activity Markings

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

The clarifications in the spec removed the two open scope questions, so this phase records the
construction decisions rather than resolving unknowns.

## R-001: A beer day is its own aggregate, not a field on a logged day

**Decision**: `BeerDay` is a new aggregate root in `DietApi.Domain`, one row per `(DietPlanId, Date)`,
carrying nothing but the date. It is created by marking and destroyed by unmarking.

**Rationale**: Exactly the reasoning that made `ExerciseDay` a sibling of `LoggedDay` (002 R-002).
`LoggedDay` is created lazily by the first food entry and deleted when the last is removed. A beer
marker living there would vanish when a member cleared that day's food, and a beer day with no food
logged (an explicit edge case, and a common real one) would have nowhere to live. FR-004 also says a
beer day carries no calories and does not touch the eating verdict — keeping it out of `LoggedDay`
makes that structural rather than a rule an implementer must remember.

**Alternatives considered**: a boolean on `LoggedDay` (rejected: lifecycle, and it would drag beer
into `Assess()`); a generic "day tag" table (rejected: no second tag is planned, and a typed
aggregate gets typed rules and tests).

## R-002: No concurrency token on `BeerDay`

**Decision**: `BeerDay` has no `Version`. Marking is create-if-absent, unmarking is delete-if-present,
both idempotent. Two devices marking the same day converge; a mark racing an unmark resolves to
whichever committed last, and the member can see and redo either.

**Rationale**: `LoggedDay` and `ExerciseDay` need a token because they accumulate child entries whose
stored totals must not diverge from a stale write. A beer day has no mutable state to corrupt — it
either exists or does not. A token here would be ceremony with nothing to protect.

## R-003: Calendar fetches a third independent range and merges

**Decision**: The diet calendar calls `/api/beer-days?from=&to=` alongside the eating range and the
exercise range, and merges the three client-side, exactly as it already merges eating and exercise
(002 R-005).

**Rationale**: The eating contract stays unaware of beer, the same way it is unaware of exercise. The
beer endpoint returns only the dates that are beer days in the window (within plan, not future);
absence is "not a beer day", with no third state invented — the pattern the exercise range already
follows.

## R-004: The calendar keeps eating state as the fill; beer and exercise are on-cell indicators

**Decision** (Q2 → A): the day cell's background stays the eating-state colour. Exercise keeps its
top-right dot; beer gets a **bottom-right bar**. The two markings differ in position, in shape, and
in hue, so they are distinguishable without relying on colour (SC-005).

**Rationale**: The calendar's existing design note is explicit that a marking must not replace the
eating colour, "so both facts stay visible". Q2 → A endorses that. Two corner indicators of
different shape sit on a ~21px year-view cell without collision.

**Colour tokens**: component CSS must not carry raw hex (Constitution, and `styles.scss`'s own note).
Two tokens are added to `styles.scss` with light and dark values: `--exercise-mark` (reusing the
existing informational blue) and `--beer-mark` (a violet, distinct from teal on-target, amber
over-target, and blue exercise). The exercise dot moves from `--text` to `--exercise-mark`; its
`--surface` ring is kept so it still reads on every fill.

## R-005: Marking happens through a day popover on the calendar

**Decision**: Clicking a within-plan day opens a small modal (the pattern the quit-smoking calendar
already uses) with the date, a "🍺 Beer day" toggle, a note of any exercise recorded, and an "Open
food log" button that navigates to `/diet/log/:date` as the cell click does today.

**Rationale**: FR-001 and SC-001 require marking *from the calendar*, in under 10 seconds. A toggle
in the cell itself does not fit beside two indicators and a date number at year-view size. A modal is
already an established pattern in the sibling programme's calendar, keeps the interaction on the
calendar, and leaves the existing "open the day" path intact.

## R-006: The analytics figure is a new read, computed by a pure domain service

**Decision**: A new `GET /api/diet-analytics/habits` endpoint. A `HabitAnalyser` domain service takes
the resolved `AnalysisPeriod`, the member's logged-day rows (already available from
`IDietAnalyticsRepository.GetDayRowsAsync`), the set of beer dates, and the set of exercise dates,
and returns a `HabitAnalysis` value object: beer-day count and per-week rate, exercise-day count and
per-week rate, and the eating-outcome split (on target / over target / not logged) for beer days
versus every other in-plan day.

**Rationale**: Principle II — the arithmetic and the comparison are judgements, so they live in the
domain and are tested without a database. The handler only gathers inputs from three repositories and
maps the result. Not-logged days are in-plan days in the period with no logged row and no beer/
exercise-only bias: they are derived in the analyser from the period bounds, not queried.

**Alternatives considered**: extending the existing intake response (rejected: four independent
analytics calls is the established shape, each section renders and tests independently); a SQL view
joining three tables (rejected: crosses no boundary but buries a judgement in infrastructure).

## R-007: No new service, port, database, or compose change

Everything lands in `DietApi` / `diet.db` and the existing Angular diet programme, exactly as 002 and
003 did. One migration, `AddBeerDays`, adding one table. No entry in `docker-compose.yml`, no new
frontend path prefix — one new client-side interaction inside `/diet/*`.
