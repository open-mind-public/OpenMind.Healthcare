# REST API Contract: Diet Tracking

Conventions, auth, status codes, and serialization: [README.md](./README.md). Field types and
rules: [../data-model.md](../data-model.md).

All paths below are as mounted on the service. The browser reaches them through the `/diet-api`
prefix.

---

## Plan — `/api/diet-plan`

### `GET /api/diet-plan`

The member's plan. **404** when they have none — the client routes to setup.

```json
{
  "id": "…", "goal": "LoseWeight", "startDate": "2026-09-01",
  "bodyMetrics": { "heightCm": 178.0, "age": 34, "sex": "Male" },
  "activityLevel": "ModeratelyActive",
  "targets": { "calories": 2100, "proteinG": 157.5, "carbsG": 210.0, "fatG": 70.0 },
  "targetSource": "Suggested",
  "targetWeightKg": 78.0,
  "currentWeightKg": 84.2,
  "createdAt": "2026-09-01T09:12:04Z", "updatedAt": "2026-09-02T07:40:11Z"
}
```

*US1 · FR-001, FR-006, FR-011*

### `POST /api/diet-plan/target-suggestion`

Calculates a suggestion and **persists nothing**. Used during setup before a plan exists, and again
whenever body details change (FR-009). Every intermediate is returned so the UI can explain the
number instead of asserting it.

Request: `{ "goal", "bodyMetrics": { "heightCm", "age", "sex" }, "currentWeightKg", "activityLevel" }`

```json
{
  "suggestedTargets": { "calories": 2100, "proteinG": 157.5, "carbsG": 210.0, "fatG": 70.0 },
  "restingEnergyKcal": 1789, "activityAdjustedKcal": 2773, "goalAdjustmentKcal": -500,
  "wasClampedToFloor": false, "floorKcal": 1500,
  "disclaimer": "A suggestion based on general guidance, not medical advice. You can set your own target."
}
```

*US1 · FR-005, FR-007, FR-010*

### `POST /api/diet-plan`

Creates the plan. **201** with `Location`. **400** if one already exists (FR-001), or on any rule
break. The supplied `currentWeightKg` is stored as the first weight reading, so current weight has
one source of truth (FR-017).

Request: `{ "goal", "startDate", "bodyMetrics", "activityLevel", "currentWeightKg", "targetWeightKg?", "targets", "targetSource" }`

Response: the plan, plus `"belowSafeFloorWarning": "…"` when `targetSource` is `MemberSet` and the
calorie target is under the floor. The warning accompanies a **successful** create — the override
is allowed, not blocked (FR-008).

*US1 · FR-001 to FR-003, FR-006 to FR-008*

### `PUT /api/diet-plan`

Updates goal, start date, body details, activity level, and target weight. **Does not change
targets** — it returns a refreshed suggestion alongside the updated plan and leaves the target in
force untouched until the member confirms it (FR-009).

```json
{ "plan": { … }, "refreshedSuggestion": { … }, "targetsUnchanged": true }
```

*US1 · FR-004, FR-009*

### `PUT /api/diet-plan/targets`

The only way targets change. Request: `{ "targets", "targetSource" }`. Returns the plan, with
`belowSafeFloorWarning` when applicable. Days already logged keep their snapshot (FR-004, R-006).

*US1 · FR-004, FR-006 to FR-008*

---

## Food log — `/api/food-log`

### `GET /api/food-log/{date}`

One day, whether or not it has been logged. A date with no entries returns `state: "NotLogged"`
with zero totals and an empty list — not a 404 (R-008). A date **before the plan start or in the
future** is refused with **400**; the three-member `DayState` never has to describe a day outside
the plan (FR-032). `version` is the day's concurrency token and must be echoed back on any write
(FR-045).

```json
{
  "date": "2026-09-02", "state": "OverTarget", "version": "8f2c…",
  "targets": { "calories": 2100, … },
  "totals": { "calories": 2340, "proteinG": 120.5, "carbsG": 250.0, "fatG": 88.0 },
  "remainingCalories": -240, "overageCalories": 240,
  "entries": [
    { "id": "…", "mealType": "Breakfast", "foodName": "Porridge oats",
      "servingLabel": "1 bowl (60 g)", "quantity": 1.0,
      "nutrition": { "calories": 228, "proteinG": 8.4, "carbsG": 36.0, "fatG": 4.8 },
      "foodLibraryItemId": "…", "servingSizeId": "…", "loggedAt": "2026-09-02T07:15:00Z" }
  ]
}
```

*US2 · FR-023, FR-031 to FR-033*

### `GET /api/food-log?from={date}&to={date}`

Day summaries for a range — the calendar's data source. Returns one small row per day, never the
entries, which is what keeps a three-year history under a second (R-010). Days outside the plan
carry `withinPlan: false` and no `state`, so the calendar renders them as neither success nor miss
without inventing a fourth `DayState` (FR-036).

```json
{ "from": "2026-08-28", "to": "2026-09-30", "planStartDate": "2026-09-01",
  "days": [
    { "date": "2026-08-28", "withinPlan": false },
    { "date": "2026-09-01", "withinPlan": true, "state": "OnTarget",
      "consumedCalories": 1980, "targetCalories": 2100 } ] }
```

*US3 · FR-034, FR-036*

### `POST /api/food-log/{date}/entries`

Adds an entry. Creates the day if this is its first entry, snapshotting the plan's current targets
onto it (R-006). Nutrition is read from the library item's chosen serving, multiplied by quantity,
and snapshotted onto the entry (R-005).

Request: `{ "foodLibraryItemId", "servingSizeId", "quantity", "mealType", "version" }` — `version`
is omitted when the date has no day yet, and required otherwise.

Returns the full day with a new `version`, so the client shows updated totals from one round trip
(SC-005). **400** on a future date, a date before plan start, a non-positive quantity, or an entry
over the calorie ceiling. **404** if the food or serving does not exist. **409** if `version` is
stale — another session changed the day first (FR-045).

*US2 · FR-023 to FR-027, FR-030*

### `PUT /api/food-log/entries/{entryId}`

Request: `{ "servingSizeId", "quantity", "mealType", "version" }`. Re-reads nutrition from the
library for the serving now named and re-snapshots it — a member's own edit is deliberate, unlike a
background library correction (FR-025, R-005). Returns the full day with a new `version`. **404** if
the entry is not the caller's. **409** on a stale `version`.

*US2 · FR-028*

### `DELETE /api/food-log/entries/{entryId}`

Takes `?version=` as a query parameter. Removes the entry and recomputes totals. If it was the day's
last entry, the day is deleted and the date returns to `NotLogged` (R-008). Returns the day with a
new `version`, or **204** when the day no longer exists. **409** on a stale `version`.

*US2 · FR-028, FR-032*

---

## Weight — `/api/weight`

### `GET /api/weight?from={date}&to={date}`

Readings plus the derived trend. Empty readings is a 200 with an empty list (FR-018).

```json
{ "readings": [ { "date": "2026-09-01", "weightKg": 84.6 } ],
  "startWeightKg": 84.6, "currentWeightKg": 84.2, "changeKg": -0.4,
  "targetWeightKg": 78.0, "remainingToTargetKg": 6.2, "goalReached": false }
```

*US4 · FR-015, FR-016, FR-018*

### `PUT /api/weight/{date}`

Records or replaces the reading for that date — idempotent by design, since a date holds at most
one (FR-012). Request: `{ "weightKg" }`. **400** on a future date or an implausible value.

*US4 · FR-012, FR-014, FR-017*

### `DELETE /api/weight/{date}`

**204** on success, **404** if no reading exists for that date, and **400** when it is the plan's
only remaining reading — current weight must always have a source, so a mistyped reading is
corrected with `PUT`, not deleted (FR-046, R-016).

*US4 · FR-013*

---

## Food library — `/api/food-library`

### `GET /api/food-library/search?q={text}&limit={n}`

Case-insensitive prefix-then-substring match on the normalised name, prefix matches first, capped
at 20. An empty `matches` array is the contract's way of saying the food is unavailable — the
client shows that plainly and creates nothing (FR-022).

```json
{ "query": "oat", "matches": [
  { "id": "…", "name": "Porridge oats", "category": "Staple",
    "servingSizes": [ { "id": "…", "label": "1 bowl (60 g)", "gramWeight": 60.0,
      "nutrition": { "calories": 228, "proteinG": 8.4, "carbsG": 36.0, "fatG": 4.8 } } ] } ] }
```

*US2 · FR-019, FR-020, FR-022*

### `GET /api/food-library/{id}`

One item with all its serving sizes. **404** if unknown.

*US2 · FR-019*

---

## Statistics — `/api/diet-stats`

### `GET /api/diet-stats`

A member with a plan and no entries gets zeros, not an error (FR-037).

```json
{ "currentStreakDays": 5, "longestStreakDays": 12, "totalDaysLogged": 47,
  "averageDailyCalories": 2043, "averageWindowDays": 30,
  "planStartDate": "2026-07-18", "daysOnPlan": 46 }
```

Current streak counts back from today through consecutive `OnTarget` days and stops at the first
day that is `OverTarget` **or** `NotLogged`. Days before plan start and after today are excluded
entirely (R-008, FR-036).

*US3 · FR-035, FR-036, FR-037*

---

## Achievements — `/api/diet-achievements`

### `GET /api/diet-achievements`

All definitions with per-member state. `earnedOn` is persisted, so an unlocked achievement stays
unlocked even if the qualifying day is later edited away (FR-039).

```json
{ "achievements": [
  { "id": "…", "name": "Full week on target", "description": "…", "icon": "🥗",
    "criterion": "ConsecutiveOnTargetDays", "threshold": 7,
    "unlocked": true, "earnedOn": "2026-08-14", "remaining": 0 },
  { "id": "…", "name": "One hundred days logged", "criterion": "TotalDaysLogged", "threshold": 100,
    "unlocked": false, "earnedOn": null, "remaining": 53 } ] }
```

*US5 · FR-038, FR-040*

### `GET /api/diet-achievements/unlocked`

Unlocked only, newest first.

*US5 · FR-038*

### `POST /api/diet-achievements/check`

Evaluates criteria and persists any newly met achievement, returning just the new ones so the
client can celebrate them. Idempotent — calling it twice awards nothing the second time (FR-039).
Called after a day's entries change.

```json
{ "newlyUnlocked": [ { "id": "…", "name": "Full week on target", "icon": "🥗", "earnedOn": "2026-09-02" } ] }
```

*US5 · FR-038, FR-039*

---

## Guidance — `/api/diet-guidance`

### `GET /api/diet-guidance/tips?category={category}`

Curated tips, optionally filtered. `category` is one of `Craving`, `Planning`, `PortionControl`,
`EatingOut`, `Mindset`.

*US6 · FR-041*

### `GET /api/diet-guidance/encouragement`

A progress-aware message. A member with no logged days gets a getting-started message, not an error
(US6 scenario 3).

```json
{ "message": "Five days on target — that is a habit forming.", "currentStreakDays": 5, "tone": "Streak" }
```

*US6 · FR-041*

---

## Health — `/health`

### `GET /health`

Unauthenticated, outside `/api`. **200** with `{ "status": "healthy" }`. Exists because every
`Dockerfile` in this repo declares a `HEALTHCHECK` against it while no service currently maps it
(R-013).

---

## Coverage

| User story | Endpoints |
|--------|--------|
| US1 Set up a plan | `GET/POST/PUT /api/diet-plan`, `POST /api/diet-plan/target-suggestion`, `PUT /api/diet-plan/targets` |
| US2 Log meals | `GET /api/food-log/{date}`, `POST /api/food-log/{date}/entries`, `PUT`/`DELETE /api/food-log/entries/{id}`, `GET /api/food-library/search`, `GET /api/food-library/{id}` |
| US3 Review history | `GET /api/food-log?from&to`, `GET /api/diet-stats` |
| US4 Weight | `GET /api/weight`, `PUT`/`DELETE /api/weight/{date}` |
| US5 Achievements | `GET /api/diet-achievements`, `GET /api/diet-achievements/unlocked`, `POST /api/diet-achievements/check` |
| US6 Guidance | `GET /api/diet-guidance/tips`, `GET /api/diet-guidance/encouragement` |
