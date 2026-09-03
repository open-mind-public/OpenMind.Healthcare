# REST API Contract: Diet Analytics

**Feature**: [../spec.md](../spec.md) | **Data model**: [../data-model.md](../data-model.md)

These endpoints are added to `DietApi`. Conventions are inherited unchanged from the diet contract
([001 contracts/README.md](../../001-diet-tracking/contracts/README.md)) — bearer auth on every
route, the member's identity read only from the token, `DomainException` → 400, missing plan → 404,
enums as names, `DateOnly` as `"2026-09-03"`.

The browser reaches these through the existing `/diet-api` prefix. **No new frontend prefix,
compose service or port is introduced.**

| Group | Base path | Feature folder |
|--------|--------|--------|
| Diet analytics | `/api/diet-analytics` | `Features/DietAnalytics` |

Every route is **read-only**. Nothing here has a side effect, which is why they are all `GET` and
why no route takes a concurrency `version`.

## Shared parameters

| Parameter | Applies to | Notes |
|--------|--------|--------|
| `period` | all | `Week` \| `Month` \| `Quarter` \| `Plan`. Defaults to `Month`. |
| `utcOffsetMinutes` | patterns, observations | The caller's offset from UTC, e.g. `330` for +05:30. Defaults to `0`. Needed because no member timezone is stored anywhere (research R-006). |

All five routes return **404** when the member has no plan, so the client can route to setup.

## The period block

Every response carries the same `period` object, because every figure in it is only meaningful
against the window it was computed over.

```json
{
  "preset": "Month",
  "from": "2026-08-05", "to": "2026-09-03",
  "wasNarrowed": false,
  "totalDays": 30,
  "loggedDays": 24,
  "hasComparison": true,
  "previousFrom": "2026-07-06", "previousTo": "2026-08-04"
}
```

`wasNarrowed` is true when the requested window was clipped to the plan's start or to today; the
client says which period was actually analysed (FR-002). `hasComparison` is false rather than the
comparison being zeros — a window before the plan started does not exist, and reporting zeros for
it would assert the member did nothing (research R-012).

---

## `GET /api/diet-analytics/intake` — US1

Where the calories went.

```json
{
  "period": { "...": "as above" },
  "summary": {
    "totalKilocalories": 51840, "averageDailyKilocalories": 2160,
    "averagedOverDays": 24, "averagedOver": "LoggedDays",
    "previousAverageDailyKilocalories": 2240,
    "onTargetDays": 9, "overTargetDays": 15, "notLoggedDays": 6
  },
  "meals": [
    { "meal": "Breakfast", "kilocalories": 10368, "shareOfTotal": 20.0, "entryCount": 41 },
    { "meal": "Lunch", "kilocalories": 14515, "shareOfTotal": 28.0, "entryCount": 52 },
    { "meal": "Dinner", "kilocalories": 20736, "shareOfTotal": 40.0, "entryCount": 48 },
    { "meal": "Snack", "kilocalories": 6221, "shareOfTotal": 12.0, "entryCount": 63 }
  ],
  "topFoods": [
    { "foodLibraryItemId": "…", "foodName": "Porridge oats",
      "kilocalories": 5472, "shareOfTotal": 10.6, "timesLogged": 24 }
  ],
  "categories": [
    { "category": "Staple", "kilocalories": 18662, "shareOfTotal": 36.0 }
  ]
}
```

- `averagedOver` is `LoggedDays` or `AllDays`, and `averagedOverDays` is that count. Carried on the
  figure itself so a client cannot show the average without the denominator (FR-003, SC-003).
- `meals` and `categories` are **exhaustive** — every meal and every category appears, at zero if
  nothing was logged — so the shares sum to 100 and the energies sum to `totalKilocalories`
  (FR-006, SC-002).
- `topFoods` is the top ten and is **deliberately not exhaustive**. Its shares do not sum to 100
  and the contract does not pretend they do.
- `onTargetDays + overTargetDays + notLoggedDays == period.totalDays`. Note the different
  denominator from the intake average, which is the point of labelling both.

*US1 · FR-003, FR-005 to FR-009*

---

## `GET /api/diet-analytics/trend`

One point per **calendar day** in the period, for the day-by-day line chart. Reads the same per-day
rows the other sections use; no new query.

```json
{
  "period": { "...": "as above" },
  "loggedDays": 23,
  "peakKilocalories": 2300,
  "points": [
    { "date": "2026-08-05", "logged": false, "kilocalories": 0, "targetKilocalories": 2100,
      "proteinG": 0, "carbsG": 0, "fatG": 0,
      "targetProteinG": 157.5, "targetCarbsG": 210, "targetFatG": 70 },
    { "date": "2026-08-10", "logged": true, "kilocalories": 900, "targetKilocalories": 2100,
      "proteinG": 148.5, "carbsG": 33, "fatG": 23,
      "targetProteinG": 157.5, "targetCarbsG": 210, "targetFatG": 70 }
  ]
}
```

- **`logged` is the field a chart must read first.** Days the member did not log are present so the
  x-axis stays a real calendar — omitting them would compress time and make a fortnight of neglect
  look like a continuous run — but their intake figures are placeholders, not measurements. A line
  must **break** at them rather than pass through, which would draw intake that never happened.
- The **target is carried across gaps** and is continuous. A member's target was in force whether
  or not they logged; not logging does not suspend it. Intake is never carried forward this way.
- Before the first logged day there is no stored target, so the earliest known one is used.
- Macronutrient targets are null when the plan sets none, and the chart draws no reference line for
  that series (FR-012).

*Feeds the day-by-day chart on the analytics page. Same data as `/intake`, laid out over time.*

---

## `GET /api/diet-analytics/macros` — US2

```json
{
  "period": { "...": "as above" },
  "averagedOverDays": 24,
  "hasTargets": true,
  "actual":  { "proteinG": 118.4, "carbsG": 244.9, "fatG": 79.2 },
  "target":  { "proteinG": 157.5, "carbsG": 210.0, "fatG": 70.0 },
  "shareOfEnergy": { "protein": 21.9, "carbs": 45.4, "fat": 33.0 }
}
```

`actual` and `target` are **daily averages over logged days**, and `target` is the average of each
day's own stored target — not the plan's current one. A period spanning a target change is
therefore judged against what was actually in force (FR-011, SC-006).

`hasTargets` false means `target` is null and the split is presented without a comparison
(FR-012). The client must not substitute the plan's present target in that case.

*US2 · FR-010 to FR-012*

---

## `GET /api/diet-analytics/patterns` — US3

```json
{
  "period": { "...": "as above" },
  "utcOffsetMinutes": 330,
  "isApproximate": true,
  "approximationReason": "Times are when an entry was recorded, not necessarily when it was eaten.",
  "byWeekday": [
    { "dayOfWeek": "Monday", "averageKilocalories": 2040, "loggedDays": 4 }
  ],
  "byHour": [
    { "hour": 8, "kilocalories": 8200, "shareOfTotal": 15.8 }
  ]
}
```

- `byWeekday` has seven entries and comes from each logged day's calendar date, which carries no
  timezone. A weekday with nothing logged reports zero with `loggedDays: 0`.
- `byHour` has twenty-four entries in the **caller's** local hours, rotated from quarter-hour
  buckets so offsets of +05:30 and +05:45 land exactly rather than approximately (research R-006).
- `isApproximate` and `approximationReason` are always present and always true for this resource.
  FR-015 requires the screen to say so, and the contract makes that impossible to forget.

*US3 · FR-013 to FR-015*

---

## `GET /api/diet-analytics/observations` — US4

```json
{
  "period": { "...": "as above" },
  "observations": [
    { "family": "Timing", "text": "About a third of what you logged this month was after 9pm.",
      "figure": "32%", "basedOnDays": 24, "strength": 0.71 }
  ],
  "nothingStoodOut": false,
  "minimumDaysForAnyObservation": 14
}
```

- `observations` is ordered strongest first, carries at most one entry per `family` (FR-022), and
  is **empty when nothing met its threshold** — which is an answer, signalled by
  `nothingStoodOut: true` rather than by an empty list the client has to interpret (FR-021).
- Every observation carries the `figure` it rests on and the `basedOnDays` behind it (FR-017).
- `minimumDaysForAnyObservation` lets the client tell a member with nine logged days *why* they see
  nothing, instead of showing them an unexplained blank (FR-018).
- The same data over the same period returns the identical list every time (FR-020). Nothing here
  is sampled, ranked randomly, or time-dependent beyond the period itself.

*US4 · FR-016 to FR-022*

---

## What this contract deliberately does not do

These are guarantees, not omissions. Each corresponds to a requirement, and any of them appearing
in a later revision would be a regression:

| Not present | Why |
|--------|--------|
| No field combining exercise energy with intake or a target — no `net`, no `available`, no `burned` offsetting `consumed` | FR-023 — carried forward from 002, where the whole feature was shaped around it. A structural test asserts it |
| No `POST`, `PUT`, `PATCH` or `DELETE` on any analytics route | FR-024 — viewing analytics cannot change anything, and there is no verb here that could |
| No average without its denominator beside it | FR-003 — the two travel together in one object so they cannot be separated by a client |
| No observation without its figure and its day count | FR-017 — a claim with no number attached is not checkable by the member |
| No arbitrary `from`/`to` date range | Deferred by the spec. Presets only in this release; the period is resolved in one place, so adding it later touches one service |
| No member timezone read from another service | Principle I — the offset arrives in the request instead |

## Coverage

| User story | Endpoint |
|--------|--------|
| US1 Where the calories go | `GET /api/diet-analytics/intake`, `GET /api/diet-analytics/trend` |
| US2 Targets and macronutrients | `GET /api/diet-analytics/macros` |
| US3 When I eat | `GET /api/diet-analytics/patterns` |
| US4 What was noticed | `GET /api/diet-analytics/observations` |

Five endpoints rather than one composite response, so each user story ships and is testable on its
own, and the page renders each section as it arrives. `/trend` was added after the first release to
carry the day-by-day chart; it belongs to US1, reads no new data, and introduces no new query.
