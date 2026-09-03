# REST API Contract: Exercise Logging

**Feature**: [../spec.md](../spec.md) | **Data model**: [../data-model.md](../data-model.md)

These endpoints are added to `DietApi`. Conventions are inherited unchanged from the diet contract
([001 contracts/README.md](../../001-diet-tracking/contracts/README.md)) — bearer auth on every
route, the member's identity read only from the token, `DomainException` → 400, missing plan → 404,
stale `version` → 409, enums as names, `DateOnly` as `"2026-09-03"`.

The browser reaches these through the existing `/diet-api` prefix. **No new frontend prefix, compose
service or port is introduced.**

| Group | Base path | Feature folder |
|--------|--------|--------|
| Exercise | `/api/exercise` | `Features/Exercise` |
| Activity catalogue | `/api/activity-catalogue` | `Features/ActivityCatalogue` |

---

## Exercise — `/api/exercise`

### `GET /api/exercise/{date}`

One day's recorded activity. A date with nothing recorded returns an empty day, not a 404 — the
same shape as the food log. **400** for a date in the future or before the plan started; **404**
when the member has no plan.

```json
{
  "date": "2026-09-03",
  "version": "3a1f…",
  "totalMinutes": 75,
  "totalKilocalories": 512,
  "entries": [
    { "id": "…", "activityTypeId": "…", "activityName": "Running, 8 km/h",
      "met": 8.3, "durationMinutes": 45, "estimatedKcal": 436,
      "recordedAt": "2026-09-03T07:20:00Z" },
    { "id": "…", "activityTypeId": "…", "activityName": "Walking, brisk",
      "met": 4.3, "durationMinutes": 30, "estimatedKcal": 76,
      "recordedAt": "2026-09-03T18:05:00Z" }
  ]
}
```

`version` is null when no day exists yet, and must be echoed back on any write to an existing day.

*US1, US2 · FR-001, FR-004, FR-016, FR-020*

### `GET /api/exercise?from={date}&to={date}`

One summary row per day with activity, for the calendar. Returns only days that have entries —
absence means no exercise, which is what lets the calendar mark days without inventing a state.

```json
{ "from": "2026-09-01", "to": "2026-09-30",
  "days": [ { "date": "2026-09-03", "totalMinutes": 75, "totalKilocalories": 512, "entryCount": 2 } ] }
```

> Deliberately **separate** from `GET /api/food-log`. The eating contract is unchanged and knows
> nothing about exercise; the calendar fetches both and merges. See research R-005.

*US3 · FR-021, FR-023*

### `GET /api/exercise/summary`

The weekly picture, with the previous window for comparison. Zeros, not an error, for a member with
no activity.

```json
{ "windowDays": 7, "activeDays": 4, "totalMinutes": 210, "totalKilocalories": 1480,
  "previousWindowActiveDays": 2, "previousWindowMinutes": 95 }
```

*US4 · FR-022, FR-024*

### `POST /api/exercise/{date}/entries`

Records a session, creating the day if it is the date's first. The estimate is computed from the
activity's MET, the duration and the member's current weight, then **snapshotted** onto the entry.

Request: `{ "activityTypeId", "durationMinutes", "version" }` — `version` omitted when the date has
no day yet, required otherwise.

Returns the full day with a new `version`, so the client updates totals in one round trip (SC-002).
**400** on a future date, a date before plan start, or a non-positive/over-ceiling duration.
**404** if the activity is not in the catalogue. **409** on a stale `version`.

*US1 · FR-001 to FR-009*

### `PUT /api/exercise/entries/{entryId}`

Request: `{ "activityTypeId", "durationMinutes", "version" }`. Re-reads the activity and
re-estimates, then re-snapshots — a member's own edit is deliberate, unlike a background catalogue
correction. Returns the full day. **404** if the entry is not the caller's. **409** on a stale
`version`.

*US2 · FR-010*

### `DELETE /api/exercise/entries/{entryId}`

Takes `?version=`. Removes the entry and recomputes totals. If it was the day's last, the day is
deleted and the date returns to having no exercise. Returns the day, or **204** when the day no
longer exists. **409** on a stale `version`.

*US2 · FR-011*

---

## Activity catalogue — `/api/activity-catalogue`

### `GET /api/activity-catalogue/search?q={text}&limit={n}`

Case-insensitive prefix-then-substring match on the normalised name, capped at 20. An empty
`matches` array is how a member learns an activity is unavailable — the client says so plainly and
creates nothing (FR-027).

```json
{ "query": "run", "matches": [
  { "id": "…", "name": "Running, 8 km/h", "category": "Running", "met": 8.3 },
  { "id": "…", "name": "Running, 12 km/h", "category": "Running", "met": 11.8 } ] }
```

Intensity is expressed as separate catalogue entries rather than a field on the log — see R-003.

*US1 · FR-003, FR-026, FR-027*

---

## What this contract deliberately does not do

These are guarantees, not omissions. Each corresponds to a requirement, and any of them appearing in
a later revision would be a regression:

| Not present | Why |
|--------|--------|
| No field combining the estimate with the daily calorie target | FR-016 — the estimate is never presented as calories available to eat |
| No exercise field on any `/api/food-log` response | FR-013, R-005 — the eating contract stays unaware of exercise |
| No exercise input to `/api/diet-plan/target-suggestion` | FR-018 — logged exercise never moves the suggested target |
| No change to `GET /api/food-log/{date}`'s `state` or `targets` | FR-015, SC-008 — a day's verdict cannot move because exercise was recorded |
| No fourth `DayState` member | R-009 — exercise is an independent flag on a calendar day, not an eating state |

## Coverage

| User story | Endpoints |
|--------|--------|
| US1 Record exercise | `GET /api/exercise/{date}`, `POST /api/exercise/{date}/entries`, `GET /api/activity-catalogue/search` |
| US2 Correct it | `PUT` / `DELETE /api/exercise/entries/{id}` |
| US3 See it beside eating | `GET /api/exercise?from&to` |
| US4 See how active | `GET /api/exercise/summary` |
