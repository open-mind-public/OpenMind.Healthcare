# Quickstart: Validating Diet Tracking

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Contract**: [contracts/rest-api.md](./contracts/rest-api.md)

How to run the feature and prove it works. Request and response shapes are in the contract; field
rules are in [data-model.md](./data-model.md). Nothing is duplicated here.

## Prerequisites

- .NET 10 SDK, Node 20+, Docker Desktop (only for the container run)
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`
- Repository root: `c:\Users\tung.le\Data\git-personal\OpenMind.Healthcare`

Ports this feature claims — 3005 dev, 5436 Docker host — and everything already allocated are in
[research.md](./research.md) R-011. Check nothing else has taken them before starting.

## Build and test

```powershell
dotnet build OpenMind.Healthcare.sln
dotnet test OpenMind.Healthcare.sln
```

Both must succeed with no new warnings. `dotnet test` is only meaningful once `DietApi.Tests` and
`QuitSmokingApi.Tests` are registered in the solution (R-014) — before that it silently runs
nothing, which is the constitution's outstanding TODO.

To run this feature's tests alone:

```powershell
dotnet test OpenMind.Healthcare\backend\DietApi.Tests\DietApi.Tests.csproj
```

## Create the database

```powershell
cd OpenMind.Healthcare\backend\DietApi
dotnet ef migrations add InitialCreate -o Infrastructure/Data/Migrations
```

Migrations apply themselves at startup, so there is no separate `database update` step. To prove
Principle VI, delete `diet.db` and start the service twice: the first run creates the schema and
seeds, the second changes nothing.

## Run for development

Three services and the front end, in four terminals:

```powershell
dotnet run --project OpenMind.Healthcare\backend\UserApi          # 3004 - sign-in
dotnet run --project OpenMind.Healthcare\backend\DietApi          # 3005 - this feature
dotnet run --project OpenMind.Healthcare\backend\QuitSmokingApi   # 3003 - not needed, but proves separation
cd OpenMind.Healthcare\frontend; npm start                        # 4200
```

`UserApi` is required — every diet endpoint needs a token it issues. Browse to
<http://localhost:4200>, register or sign in, then open the diet area.

API reference for the running service: <http://localhost:3005/scalar/v1>.

## Run in containers

```powershell
cd OpenMind.Healthcare
docker compose up --build
```

Front end on <http://localhost:5435>, diet service on <http://localhost:5436>. This is the run that
catches a `/diet-api` prefix added to `proxy.conf.json` but not `nginx.conf` — the failure mode the
constitution warns about, which dev mode cannot reveal.

```powershell
docker compose ps          # diet-api should report healthy, not just running
```

## Getting a token for manual API calls

```powershell
$login = Invoke-RestMethod -Uri http://localhost:3004/api/auth/login -Method Post `
  -ContentType application/json `
  -Body '{"email":"you@example.com","password":"YourPassword1!"}'
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
```

Then call any endpoint, for example:

```powershell
Invoke-RestMethod -Uri http://localhost:3005/api/diet-plan -Headers $headers
```

## Validation scenarios

Each proves one user story end to end. Run them in order — later ones need earlier data.

### V1 — Set up a plan (US1)

1. Open the diet area with no plan. Setup appears, not a dashboard.
2. Enter goal, start date, height, age, sex, current weight, activity level. A suggested calorie
   and macro target appears, labelled a suggestion, with the resting-energy and activity figures
   shown behind it.
3. Accept it and save. Reload — the plan persists with `targetSource: "Suggested"`.
4. Change the target to your own number and save. `targetSource` becomes `"MemberSet"`.
5. Set a target below the floor (under 1,200 for female, 1,500 for male). Save succeeds **and**
   returns a warning. It is not blocked.
6. Enter a start date in the future, then a target of 0. Both are rejected with a readable message.
7. Sign out and call `GET /api/diet-plan` with no token. 401.

**Passes when**: steps 2-6 behave as described and step 7 refuses. *FR-001 to FR-011, SC-001*

### V2 — Log meals (US2)

1. Open today. Consumed reads 0, remaining equals the target.
2. Search for a common food — "oat", "chicken", "banana". Matches appear with serving sizes.
3. Add one as breakfast. Totals update without a manual refresh.
4. Add two more across lunch and dinner until you exceed the target. The day flips to over target
   and shows the overage.
5. Edit an entry's quantity, then delete another. Totals adjust each time.
6. Delete every entry. The day returns to not logged — **not** a zero-calorie on-target day.
7. Search for something absent, e.g. "kohlrabi gratin". You are told it is unavailable; nothing is
   created.
8. Try to log against tomorrow, then against a date before plan start. Both rejected.
9. Open the same day in two browser tabs. Add an entry in tab A, then add one in tab B without
   reloading. Tab B is refused with **409** and a reload message — and **tab A's entry is still
   there**. Reload tab B and reapply; both entries now exist.
10. Run the SC-004 search corpus from T053 against the seeded library and record what fraction
    returns a usable match in the first five results. The bar is 85%.

**Passes when**: step 6 returns `NotLogged`, step 7 creates nothing, step 9 loses no entry from
either tab, and step 10 clears 85%. *FR-019 to FR-033, FR-045, SC-003, SC-004, SC-005*

### V3 — History and streaks (US3)

1. Log several consecutive days on target, one over target, and leave one unlogged.
2. Open the calendar. Each day carries the matching marking; days before plan start show as outside
   the plan.
3. Switch month view to year view. The same days carry the same markings.
4. Check statistics: the current streak counts back from today and stops at the first day that is
   over target **or** unlogged; the longest streak keeps the earlier best.
5. Now lower your daily target in the plan. Re-open the calendar — **previously assessed days must
   not change state**.

**Passes when**: step 5 changes nothing historical. That is the target-snapshot guarantee (R-006).
*FR-034 to FR-037, SC-007, SC-008, SC-009*

### V4 — Weight (US4)

1. Record a weight for today. It appears in the trend.
2. Record a different weight for the same date. It replaces the first — the date holds one reading,
   never two.
3. Record readings across several past dates. The trend is date-ordered and shows change since plan
   start and distance to target.
4. Try a future date, then 900 kg. Both rejected.
5. Delete a reading. It disappears from the trend.
6. Delete readings until one remains, then try to delete that one. It is refused with a message
   saying you can correct it instead — current weight must always have a source.
7. Update your plan's body details. The refreshed suggestion uses your newest weight, and your
   target in force is **unchanged** until you confirm.

**Passes when**: step 2 replaces, step 6 refuses, and step 7 does not silently overwrite.
*FR-009, FR-012 to FR-018, FR-046*

### V5 — Achievements (US5)

1. With a week of on-target days logged, call `POST /api/diet-achievements/check`. The week
   achievement unlocks with today's date.
2. Call it again. Nothing new is awarded.
3. Delete entries so a qualifying day no longer qualifies, then check again. **The achievement
   stays unlocked.**
4. View all achievements. Locked ones show what remains.

**Passes when**: step 3 does not revoke. *FR-038 to FR-040*

### V6 — Guidance (US6)

1. Open guidance. Tips are returned from the seeded library.
2. Request encouragement while on a streak. The message reflects it.
3. As a member with no logged days, request encouragement. A getting-started message, not an error.

*FR-041*

### V7 — Separation and idempotency (constitution)

1. Stop `QuitSmokingApi`. Every diet capability still works. Restart it and stop `DietApi` — the
   smoking area still works.
2. Confirm `diet.db`, `quitSmoking.db`, and `users.db` are three separate files with three separate
   volumes.
3. Restart `DietApi` twice. The food library, tips, and achievement definitions hold exactly one
   copy of each — not two.
4. Confirm no diet table holds a foreign key into another service's data.

**Passes when**: step 1 shows neither service takes the other down and step 3 finds no duplicates.
*FR-042 to FR-044, SC-011, SC-012, Principles I and VI*

### V8 — Scale (SC-006)

Seed roughly three years of daily entries for one member, then open the calendar year view, the
weight trend, and statistics. Each must return in under a second. This is the scenario the
aggregate split (R-004) and the per-day stored totals (R-010) exist to satisfy — if it fails, those
decisions need revisiting, not the success criterion.

## Definition of done

The constitution's gates, all of which must hold:

- [ ] `dotnet build OpenMind.Healthcare.sln` — no new warnings
- [ ] `dotnet test OpenMind.Healthcare.sln` — passes, and actually runs the diet tests
- [ ] `npm run build` in `frontend/` — succeeds
- [ ] Every new endpoint appears in Scalar and requires authorization
- [ ] A migration exists for every model change; an empty database yields a working seeded schema
- [ ] V1 to V8 above all pass, including the 409 conflict check (V2 step 9) and the search corpus (V2 step 10)
- [ ] Three ADRs exist under `OpenMind.Healthcare/adrs/` for R-004, R-007, and R-010 (T124)
- [ ] Constitution compliance re-checked; any deviation is in the plan's Complexity Tracking or
      removed
