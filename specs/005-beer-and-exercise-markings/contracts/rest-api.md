# Contract: Beer Days & Habit Analytics REST API

**Feature**: [spec.md](../spec.md) | **Plan**: [plan.md](../plan.md)

All routes are under `DietApi`, require a bearer token (`.RequireAuthorization()`), and resolve the
member from the JWT via `IUserService` — never from the request. Enums cross the wire as names.
Dates are `yyyy-MM-dd`. A member with no diet plan gets `404` from every route here.

---

## `GET /api/beer-days?from={date}&to={date}`

The beer days in a window, for the calendar.

**Query**: `from`, `to` (inclusive, `DateOnly`).

**200**:

```json
{
  "from": "2026-08-08",
  "to": "2026-09-06",
  "days": ["2026-08-09", "2026-08-16", "2026-08-30"]
}
```

`days` contains only dates that are beer days, that fall on/after the plan start, and that are not in
the future. Absence of a date means "not a beer day" — there is no third state (research.md R-003).
An empty `days` is a normal answer, not an error.

**404**: the member has no diet plan.

---

## `PUT /api/beer-days/{date}`

Mark a date as a beer day. Idempotent: marking an already-marked day succeeds and changes nothing
(FR-017).

**200**:

```json
{ "date": "2026-09-05", "isBeerDay": true }
```

**400** (`{ "message": "..." }`): the date is in the future, or before the plan started (FR-003).

**404**: the member has no diet plan.

---

## `DELETE /api/beer-days/{date}`

Remove a beer-day marking. Idempotent: unmarking a date that is not a beer day succeeds.

**204**: the date is not (or is no longer) a beer day.

**404**: the member has no diet plan.

---

## `GET /api/diet-analytics/habits?period={preset}`

Beer and exercise frequency over the selected analytics period, and how eating outcomes on beer days
compare with other days.

**Query**: `period` — `Week` | `Month` | `Quarter` | `Plan` (default `Month`). Same presets, same
clamping to plan start and today, as every other analytics route.

**200**:

```json
{
  "period": {
    "preset": "Month", "from": "2026-08-08", "to": "2026-09-06",
    "wasNarrowed": false, "totalDays": 30, "loggedDays": 22,
    "hasComparison": true, "previousFrom": "2026-07-09", "previousTo": "2026-08-07"
  },
  "inPlanDays": 30,
  "beerDays": 6,
  "beerDaysPerWeek": 1.4,
  "exerciseDays": 11,
  "exerciseDaysPerWeek": 2.6,
  "onBeerDays": {
    "days": 6, "onTargetDays": 1, "overTargetDays": 4, "notLoggedDays": 1,
    "onTargetShare": 0.17, "overTargetShare": 0.67, "notLoggedShare": 0.17
  },
  "onNonBeerDays": {
    "days": 24, "onTargetDays": 14, "overTargetDays": 6, "notLoggedDays": 4,
    "onTargetShare": 0.58, "overTargetShare": 0.25, "notLoggedShare": 0.17
  }
}
```

- `beerDays` + `onNonBeerDays.days` == `inPlanDays`; `onBeerDays.days` == `beerDays`.
- Every `*Days` triple within an outcome sums to that outcome's `days`.
- A period with zero beer days returns `beerDays: 0`, `beerDaysPerWeek: 0.0`, and `onBeerDays` all
  zeros — the section is populated, not omitted (FR-015).
- `exerciseDays` counts days with any recorded session, matching the calendar's exercise marking.

**404**: the member has no diet plan.

**Read-only**: like every `/api/diet-analytics/*` route, this is a `GET` and changes nothing.

---

## What these contracts deliberately do not carry

- No amount of beer, and no calorie figure for beer, anywhere (FR-004).
- The beer endpoints carry no eating state, target, or verdict — a client cannot combine this shape
  with the eating assessment.
- The habits response carries no "net" figure and no advice — consistent with the analytics feature's
  scope (003).
