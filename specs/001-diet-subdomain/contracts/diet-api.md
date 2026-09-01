# Contract: DietApi HTTP surface

**Feature**: `001-diet-subdomain` | **Date**: 2026-09-01

Base URL — dev `http://localhost:3005`, container `http://diet-api:5000`, browser `/diet-api/*`
(rewritten to `/api/*` by the proxy, per [research R5](../research.md#r5-frontend-routing-for-a-third-backend)).

**Every route requires authorization.** Groups are declared with `.RequireAuthorization()`; the
person is resolved from the `NameIdentifier` claim via `IUserService`. No route accepts a user id in
its path, query, or body (Constitution Principle I).

**Conventions inherited from the existing services**

- Enums cross the wire as names (`"Breakfast"`, not `0`).
- Dates are `DateOnly` (`2026-09-01`); instants are UTC `DateTime`.
- `DomainException` → `400` with `{ "message": "..." }`.
- Missing resource → `404` with no body. Successful delete → `204`.
- Unauthenticated → `401` from the JWT middleware.
- All masses in responses are metric; the client converts for display per the profile's
  `unitSystem`.

---

## `/api/diet/profile` — profile and targets

| Method | Route | Name | Purpose |
|---|---|---|---|
| `GET` | `/api/diet/profile` | `GetDietProfile` | Current profile, target, and safety-floor state. `404` when setup has not been done. |
| `POST` | `/api/diet/profile` | `CreateOrUpdateDietProfile` | Complete or amend setup; recomputes the derived target. |
| `PUT` | `/api/diet/profile/target` | `OverrideDietTarget` | Manual target override (FR-003). |
| `POST` | `/api/diet/profile/target/acknowledge` | `AcknowledgeBelowFloorTarget` | Records acknowledgement of a below-floor target (FR-004a). |

**`POST /api/diet/profile` request**

```jsonc
{
  "dateOfBirth": "1990-04-12",
  "sex": "Female",                    // Female | Male
  "heightCm": 168,
  "currentWeightKg": 72.5,
  "activityLevel": "ModeratelyActive",
  "unitSystem": "Metric",             // Metric | Imperial
  "goalDirection": "Lose",            // Lose | Maintain | Gain
  "goalWeightKg": 65,
  "weeklyRateKg": 0.5
}
```

**`GET /api/diet/profile` response**

```jsonc
{
  "startedOn": "2026-09-01",
  "sex": "Female",
  "heightCm": 168,
  "activityLevel": "ModeratelyActive",
  "unitSystem": "Metric",
  "target": {
    "energyKcal": 1685,
    "proteinPercent": 30, "carbohydratePercent": 40, "fatPercent": 30,
    "proteinG": 126.4, "carbohydrateG": 168.5, "fatG": 56.2,
    "origin": "Derived"
  },
  "safetyFloor": {
    "floorKcal": 1200,
    "isBelowFloor": false,
    "acknowledgedAt": null,
    "message": null                   // populated only when isBelowFloor is true
  },
  "goal": {
    "targetWeightKg": 65, "direction": "Lose", "weeklyRateKg": 0.5,
    "setOn": "2026-09-01", "achievedOn": null
  },
  "derivation": "Resting metabolic rate 1450 kcal (Mifflin-St Jeor), × 1.55 for moderate activity = 2248 kcal, − 550 kcal for 0.5 kg per week = 1698 kcal."
}
```

`derivation` satisfies FR-002's plain-language explanation. `isBelowFloor: true` never blocks the
response — it carries the warning text for the client to surface (FR-004).

---

## `/api/diet/foods` — catalog and custom foods

| Method | Route | Name | Purpose |
|---|---|---|---|
| `GET` | `/api/diet/foods?search=&category=&take=` | `SearchFoods` | Catalog plus the caller's own foods. Never another person's (FR-011). |
| `GET` | `/api/diet/foods/{id}` | `GetFood` | |
| `POST` | `/api/diet/foods` | `CreateCustomFood` | FR-010 |
| `PUT` | `/api/diet/foods/{id}` | `UpdateCustomFood` | Caller's own foods only; `404` for catalog items. |
| `DELETE` | `/api/diet/foods/{id}` | `DeleteCustomFood` | Never cascades to logged entries (FR-012). |

**Food representation**

```jsonc
{
  "id": "…", "name": "Rolled oats", "category": "Grain",
  "referenceUnit": "Gram",            // facts are per 100 of this unit
  "isCustom": false,
  "facts": { "energyKcal": 379, "proteinG": 13.2, "carbohydrateG": 67.7, "fatG": 6.5 }
}
```

---

## `/api/diet/log` — daily logging

| Method | Route | Name | Purpose |
|---|---|---|---|
| `GET` | `/api/diet/log/{date}` | `GetLoggedDay` | Totals, remaining, breakdown by occasion. |
| `GET` | `/api/diet/log?from=&to=` | `GetLoggedDays` | Day summaries across a range. |
| `POST` | `/api/diet/log/{date}/entries` | `AddLogEntry` | FR-014 |
| `PUT` | `/api/diet/log/{date}/entries/{entryId}` | `AmendLogEntry` | FR-015 |
| `DELETE` | `/api/diet/log/{date}/entries/{entryId}` | `RemoveLogEntry` | FR-015 |

**`POST .../entries` request**

```jsonc
{ "foodId": "…", "quantity": { "amount": 80, "unit": "Gram" }, "occasion": "Breakfast" }
```

**`GET /api/diet/log/{date}` response**

```jsonc
{
  "date": "2026-09-01",
  "status": "Logged",                 // Logged | Unlogged | OutsideTrackedPeriod
  "target": { "energyKcal": 1685, "proteinG": 126.4, "carbohydrateG": 168.5, "fatG": 56.2 },
  "totals":    { "energyKcal": 1240, "proteinG": 88.1, "carbohydrateG": 120.4, "fatG": 41.0 },
  "remaining": { "energyKcal": 445,  "proteinG": 38.3, "carbohydrateG": 48.1,  "fatG": 15.2 },
  "byOccasion": [
    { "occasion": "Breakfast", "energyKcal": 420, "entries": [
        { "id": "…", "foodId": "…", "foodName": "Rolled oats",
          "quantity": { "amount": 80, "unit": "Gram" },
          "energyKcal": 303, "proteinG": 10.6, "carbohydrateG": 54.2, "fatG": 5.2,
          "recordedAt": "2026-09-01T07:14:00Z" }
      ] }
  ]
}
```

`status` is the three-state answer FR-019 requires. `Unlogged` and `OutsideTrackedPeriod` return
`totals: null` — **not** zeros. `remaining` may be negative.

`foodName` comes from the entry's snapshot, so it stays correct after the food is renamed or deleted
(FR-012). `foodId` may be `null` for an entry whose food was later deleted.

---

## `/api/diet/weight` — weigh-ins and goal

| Method | Route | Name | Purpose |
|---|---|---|---|
| `GET` | `/api/diet/weight?from=&to=` | `GetWeighIns` | |
| `POST` | `/api/diet/weight` | `RecordWeighIn` | Amends on a repeat date (FR-021). |
| `DELETE` | `/api/diet/weight/{date}` | `RemoveWeighIn` | |
| `GET` | `/api/diet/weight/progress` | `GetWeightProgress` | FR-022, FR-023 |
| `PUT` | `/api/diet/weight/goal` | `SetWeightGoal` | FR-024 |

**`GET /api/diet/weight/progress` response**

```jsonc
{
  "startingWeightKg": 72.5, "currentWeightKg": 70.1, "goalWeightKg": 65,
  "totalChangeKg": -2.4, "changeLast30DaysKg": -1.1, "remainingToGoalKg": 5.1,
  "trend": "Improving",               // Improving | Stable | Worsening | NotEnoughData
  "goalAchieved": false,
  "weighInCount": 9,
  "latestWeighInOn": "2026-08-30"
}
```

`trend` reuses the vocabulary of the existing relapse analytics, including `NotEnoughData` — which
is what FR-023 and FR-035 require instead of a misleading reading from one data point.

---

## `/api/diet/recipes` — recipes

| Method | Route | Name |
|---|---|---|
| `GET` | `/api/diet/recipes` | `GetRecipes` |
| `GET` | `/api/diet/recipes/{id}` | `GetRecipe` |
| `POST` | `/api/diet/recipes` | `CreateRecipe` |
| `PUT` | `/api/diet/recipes/{id}` | `UpdateRecipe` |
| `DELETE` | `/api/diet/recipes/{id}` | `DeleteRecipe` |

```jsonc
// POST request
{ "name": "Overnight oats", "servings": 2,
  "ingredients": [ { "foodId": "…", "quantity": { "amount": 160, "unit": "Gram" } } ] }

// response adds:
"perServing": { "energyKcal": 303, "proteinG": 10.6, "carbohydrateG": 54.2, "fatG": 5.2 }
```

---

## `/api/diet/plans` — meal plans and shopping

| Method | Route | Name | Purpose |
|---|---|---|---|
| `GET` | `/api/diet/plans?from=&to=` | `GetMealPlans` | |
| `POST` | `/api/diet/plans` | `CreateMealPlan` | FR-026 |
| `POST` | `/api/diet/plans/{id}/meals` | `AddPlannedMeal` | |
| `DELETE` | `/api/diet/plans/{id}/meals/{mealId}` | `RemovePlannedMeal` | |
| `GET` | `/api/diet/plans/{id}/projection` | `GetPlanProjection` | FR-028 |
| `GET` | `/api/diet/plans/{id}/shopping-list` | `GetShoppingList` | FR-029, FR-030 |
| `POST` | `/api/diet/plans/{id}/confirm/{date}` | `ConfirmPlannedDay` | FR-031 |

**`GET .../projection` response** — one row per planned day:

```jsonc
{ "days": [
  { "date": "2026-09-02", "energyKcal": 1610, "targetKcal": 1685,
    "belowTargetBy": 75, "warning": null },
  { "date": "2026-09-03", "energyKcal": 900,  "targetKcal": 1685,
    "belowTargetBy": 785, "warning": "This day plans far less than your daily target." }
] }
```

The `warning` field is what makes FR-028's "warn before the day arrives" observable.

**`GET .../shopping-list` response**

```jsonc
{ "from": "2026-09-02", "to": "2026-09-08",
  "lines": [
    { "foodId": "…", "foodName": "Rolled oats", "amount": 480, "unit": "Gram" },
    { "foodId": "…", "foodName": "Onion",       "amount": 300, "unit": "Gram" },
    { "foodId": "…", "foodName": "Onion",       "amount": 2,   "unit": "Piece" }
  ] }
```

The two onion lines are the contract for FR-030: incompatible units are listed separately, never
summed. A test asserts exactly this shape.

**`POST .../confirm/{date}`** returns `200` with the created `GET /api/diet/log/{date}` payload, and
`400` when the day is already confirmed.

---

## `/api/diet/insights` — habits (P4)

| Method | Route | Name |
|---|---|---|
| `GET` | `/api/diet/insights` | `GetDietInsights` |

```jsonc
{
  "daysInPeriod": 90, "daysLogged": 61, "loggingConsistency": 0.678,
  "daysWithinTarget": 44, "adherenceRate": 0.721,
  "currentOnTargetStreak": 5, "longestOnTargetStreak": 12,
  "averageEnergyKcalLast30Days": 1712, "averageEnergyKcalPrevious30Days": 1806,
  "trend": "Improving",
  "weekdayBreakdown": [ { "weekday": "Saturday", "daysLogged": 9, "daysWithinTarget": 3, "adherenceRate": 0.333 } ]
}
```

`adherenceRate` divides by `daysLogged`, never by `daysInPeriod` — FR-034's requirement that
unlogged days are not counted as failures. A test covers a 90-day history with gaps (SC-008).

---

## Error contract

| Status | When |
|---|---|
| `400` | Any broken business rule, carried as `{ "message": "<rule's ErrorMessage>" }` |
| `401` | Missing or invalid bearer token |
| `404` | No diet profile yet; unknown food, recipe, plan, or entry; another person's private resource |
| `204` | Successful delete |

A private resource belonging to someone else returns `404`, not `403`, so the API does not disclose
that the resource exists (FR-007, FR-011).
