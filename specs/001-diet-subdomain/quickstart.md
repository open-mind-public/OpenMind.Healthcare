# Quickstart: Validating the Diet Subdomain

**Feature**: `001-diet-subdomain` | **Date**: 2026-09-01

How to run the diet capability and prove it works end to end. Entity details are in
[data-model.md](./data-model.md); request and response shapes are in
[contracts/diet-api.md](./contracts/diet-api.md).

## Prerequisites

- .NET 10 SDK, Node 18+ (the machine this was planned on has .NET 10.0.302 and Node 25.9.0)
- Docker, only for the container validation at the end

## Ports

| Service | Dev | Docker host | Browser path |
|---|---|---|---|
| QuitSmokingApi | 3003 | 5431 | `/api` |
| UserApi | 3004 | 5434 | `/user-api` |
| **DietApi** | **3005** | **5436** | **`/diet-api`** |
| Frontend | 4200 | 5435 | — |

## Run it

Four terminals from `OpenMind.Healthcare/`:

```bash
cd backend/UserApi        && dotnet run          # 3004 — needed to sign in
cd backend/QuitSmokingApi && dotnet run          # 3003
cd backend/DietApi        && dotnet run          # 3005
cd frontend               && npm start           # 4200
```

The first `dotnet run` of DietApi applies its migrations and seeds the food catalog into `diet.db`.

API reference: `http://localhost:3005/scalar/v1`.

## Build and test gates

These are the constitution's quality gates. All must pass before the feature is done.

```bash
dotnet build OpenMind.Healthcare.sln            # no new warnings
dotnet test  OpenMind.Healthcare.sln            # DietApi.Tests + QuitSmokingApi.Tests
cd OpenMind.Healthcare/frontend && npm run build
```

> `dotnet test` at solution level currently runs **nothing**, because `QuitSmokingApi.Tests` is not
> referenced by the `.sln`. Adding both test projects to the solution is a task in this feature —
> until it is done, this gate is vacuous.

---

## Validation scenarios

Get a token first — everything below needs one:

```bash
TOKEN=$(curl -s -X POST http://localhost:3004/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"you@example.com","password":"…"}' | jq -r .token)
AUTH="Authorization: Bearer $TOKEN"
```

### V1 — Daily logging (User Story 1, P1)

1. `POST /api/diet/profile` with the setup payload from the contract.
   **Expect** a derived target and a `derivation` sentence naming the RMR, the activity factor, and
   the goal adjustment.
2. `GET /api/diet/foods?search=oat` → **expect** catalog matches, none owned by another person.
3. `POST /api/diet/log/2026-09-01/entries` with 80 g of rolled oats at `Breakfast`.
   **Expect** the day's `totals.energyKcal` to equal `379 × 0.8 = 303.2` exactly — the arithmetic
   SC-003 pins down.
4. `PUT` that entry to 120 g → **expect** totals recalculate and the entry count stays at 1 (no
   duplicate, per scenario 3).
5. `POST /api/diet/foods` for something not in the catalog, then log it. **Expect** it to appear in
   your own search results afterwards.
6. `GET /api/diet/log/2000-01-01` → **expect** `status: "OutsideTrackedPeriod"` with `totals: null`.
   A response of zeros here is the FR-019 defect.
7. `GET /api/diet/log/<a date inside the period with no entries>` → **expect**
   `status: "Unlogged"`, also with `totals: null`.
8. Set a manual target below the floor (e.g. 1000 kcal for a female profile) → **expect** `200`
   with `safetyFloor.isBelowFloor: true` and a message. **Expect it not to be rejected** — FR-004
   warns, it does not block.

### V2 — Weight and goal (User Story 2, P2)

1. `POST /api/diet/weight` for three different dates.
2. `POST` again for a date already recorded → **expect** the reading to be amended, and
   `weighInCount` to stay the same (FR-021).
3. `GET /api/diet/weight/progress` → **expect** `totalChangeKg` and `remainingToGoalKg` to match
   hand arithmetic, and `trend` to be `NotEnoughData` until enough readings exist.
4. Record a reading higher than the previous one while the 30-day trend is still downward →
   **expect** `trend: "Improving"` (FR-023: the trend leads, not the last reading).
5. `POST` a weigh-in dated tomorrow → **expect** `400` with a message.
6. Record a weight at or past the goal → **expect** `goalAchieved: true`.

### V3 — Planning and shopping (User Story 3, P3)

1. `POST /api/diet/recipes` with two ingredients and `servings: 2` → **expect** `perServing` to be
   half the ingredient totals.
2. `POST /api/diet/plans`, then add meals across several days, including **the same food in four
   separate meals** and **one food in two different units**.
3. `GET .../projection` → **expect** one row per day with `targetKcal` and, on a deliberately light
   day, a non-null `warning`.
4. `GET .../shopping-list` → **expect** exactly one line for the four-times food with the summed
   amount, and **two separate lines** for the mixed-unit food (SC-005, FR-030).
5. `POST .../confirm/2026-09-02` → **expect** `200`, and `GET /api/diet/log/2026-09-02` to show
   entries identical to manual logging of the same meals (SC-007).
6. `POST` the same confirm again → **expect** `400`.
7. Leave another planned day unconfirmed and let it pass → **expect** it to contribute nothing to
   `GET /api/diet/log/{that date}` (`status: "Unlogged"`) and nothing to insights (FR-032).

### V4 — Insights (User Story 4, P4)

1. Seed a 90-day history with deliberate gaps.
2. `GET /api/diet/insights` → **expect** `adherenceRate` computed over `daysLogged`, not
   `daysInPeriod` (FR-034), and every figure to match hand calculation (SC-008).
3. Truncate history to a handful of days → **expect** `trend: "NotEnoughData"` (FR-035).

### V5 — Isolation and auth (cross-cutting)

1. Call any diet route with no token → **expect** `401`.
2. Sign in as person A, create a custom food; sign in as person B and search for it → **expect** it
   to be absent, and `GET /api/diet/foods/{id}` to return **`404`, not `403`** (FR-011).
3. Sign in once and call both `/api/progress` and `/api/diet/profile` with the same token →
   **expect** both to succeed (SC-011).
4. Stop DietApi and use the quit-smoking features → **expect** them to work unaffected (FR-038).
5. `dotnet test` for `QuitSmokingApi.Tests` → **expect** it to pass unchanged (SC-010).

### V6 — Seeding and migrations

1. Delete `diet.db`, start DietApi → **expect** migrations to apply and the catalog to populate.
2. Note the food count, restart DietApi, count again → **expect** the same number (SC-009,
   Principle VI).

### V7 — Containers

```bash
cd OpenMind.Healthcare && docker-compose up --build
```

1. `http://localhost:5436/scalar/v1` responds.
2. Through the UI at `http://localhost:5435`, exercise a diet screen → **expect** it to work.
   A 404 here while `npm start` works means `nginx.conf` was updated but `proxy.conf.json` was not,
   or the reverse — the failure mode [R5](./research.md#r5-frontend-routing-for-a-third-backend)
   exists to prevent.
3. `docker-compose down && docker-compose up` → **expect** diet data to survive in its own volume.
