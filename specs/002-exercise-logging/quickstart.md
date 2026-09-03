# Quickstart: Validating Exercise Logging

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Contract**: [contracts/rest-api.md](./contracts/rest-api.md)

How to run the feature and prove it works. Request and response shapes are in the contract; field
rules are in [data-model.md](./data-model.md). Nothing is duplicated here.

## Prerequisites

Unchanged from the diet programme — this feature adds no service, port or container. See the
[repository README](../../README.md) for ports.

- .NET 10 SDK, Node 20+
- `dotnet-ef` for the migration
- A member account with a **diet plan already set up** — exercise is bounded by the plan's start
  date and the estimate needs the plan's current weight

## Build, migrate, test

```powershell
dotnet build OpenMind.Healthcare.sln
dotnet test OpenMind.Healthcare.sln
```

```powershell
cd OpenMind.Healthcare\backend\DietApi
dotnet ef migrations add AddExerciseLogging -o Infrastructure/Data/Migrations
```

Migrations apply at startup. To prove Principle VI, delete `diet.db` and start twice: the first run
creates the schema and seeds the activity catalogue, the second changes nothing.

> If a build fails with `MSB3021 … file is locked`, a service is still running. Stop it and rebuild.

## Run

```powershell
dotnet run --project OpenMind.Healthcare\backend\UserApi     # 3004 - issues the token
dotnet run --project OpenMind.Healthcare\backend\DietApi     # 3005
cd OpenMind.Healthcare\frontend; npm start                   # 4200
```

New endpoints appear in Scalar at <http://localhost:3005/scalar/v1>.

## Validation scenarios

Each proves one user story end to end. Run in order — later ones need earlier data.

### V1 — Record exercise (US1)

1. Open **Diet → Today**. No exercise is shown, with an invitation to add some.
2. Search the catalogue for "run". Matches appear, including intensity variants as separate entries.
3. Search for something absent, e.g. "quidditch". You are told it is unavailable; nothing is created.
4. Record 45 minutes of running. It appears immediately with an estimated calorie figure, labelled
   as an estimate.
5. Record a second session the same day. **Both are kept** — the second does not replace the first,
   and the day's total is their sum.
6. Try a duration of 0, then a date in the future, then a date before your plan started. All three
   are refused with a readable message.
7. Sign out and call `GET /api/exercise/{today}` with no token. 401.

**Passes when**: step 5 keeps both entries and step 6 refuses all three. *FR-001 to FR-009, SC-001*

### V2 — Correct it (US2)

1. Change one entry's duration. The estimate and the day total both recalculate.
2. Change which activity an entry was. It updates and re-estimates.
3. Delete one of two entries. The other survives; totals adjust.
4. Delete the last entry. The date returns to **no exercise** — not a zero-minute session.
5. Open the same day in two tabs. Add an entry in tab A, then in tab B without reloading. Tab B is
   refused with **409**, and **tab A's entry is still there**.

**Passes when**: step 4 leaves no residue and step 5 loses nothing. *FR-010 to FR-012, SC-002*

### V3 — The guarantee (US1 + the diet feature)

This is the scenario the feature exists to keep true. Run it carefully.

1. Pick a past day where you were **over target** for eating. Note its target and its verdict.
2. Record a long, hard session against that same past date.
3. Re-open the day. The calorie target is **unchanged**; the day is **still over target**; and the
   estimate is shown as its own figure.
4. Confirm no screen anywhere adds the estimate to the target or offers a combined "available"
   number.
5. Open **Diet plan & targets**. Your daily target and declared activity level are unchanged, and
   the screen explains why logged exercise does not move them.

**Passes when**: nothing about the eating assessment moves, in either direction, at any point.
*FR-013 to FR-019, SC-008, SC-009*

### V4 — See it beside eating (US3)

1. Open **Diet → History** for a month containing both eating days and exercise days.
2. Days with exercise carry a distinct marking **alongside** the eating colour — neither hides the
   other.
3. A day that is on target *and* has exercise shows both facts.
4. Click a day. Its activities are listed with durations.
5. Confirm a day with exercise but **no food logged** still shows as not-logged for eating.

**Passes when**: steps 3 and 5 both hold. *FR-013, FR-021, SC-005*

### V5 — See how active (US4)

1. Open **Diet → Activity**. Active days and total time for the week are shown.
2. With several weeks of history, the current week is comparable against the previous one.
3. A week with nothing shows zeros plainly, not an error or a blank panel.

*FR-022, FR-024, SC-006*

### V6 — Catalogue and scale

1. Delete `diet.db`, start twice, and confirm exactly one copy of every activity. *SC-010*
2. Run the SC-003 search corpus against the seeded catalogue and record the hit rate. The bar is
   **85% in the first five results**. If it fails, widen the seed rather than relax the criterion.
3. Seed roughly three years of daily activity and open the calendar and the summary. Each must
   return in under a second. If this fails, the per-day aggregate and stored totals (R-002, R-004)
   are what to revisit — not the criterion. *SC-004*

## Definition of done

- [ ] `dotnet build OpenMind.Healthcare.sln` — no new warnings
- [ ] `dotnet test OpenMind.Healthcare.sln` — passes, including the day-verdict guarantee test
- [ ] `npm run build` in `frontend/` — succeeds
- [ ] New endpoints appear in Scalar and require authorization
- [ ] A migration exists; an empty database yields a working seeded schema
- [ ] V1 to V6 pass, **V3 especially**
- [ ] Constitution compliance re-checked
