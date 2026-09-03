# Quickstart: Validating Exercise Shortcuts

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Contract**: [contracts/rest-api.md](./contracts/rest-api.md)

How to run the feature and prove it works. Request and response shapes are in the contract; field
rules are in [data-model.md](./data-model.md).

## Prerequisites

Unchanged from the diet programme — no new service, port or container. See the
[repository README](../../README.md) for ports.

- .NET 10 SDK, Node 20+, `dotnet-ef` for the migration
- A member account with a **diet plan already set up** — shortcuts are bounded by the plan, and the
  estimate needs the plan's current weight

## Build, migrate, test

```powershell
dotnet build OpenMind.Healthcare.sln
dotnet test OpenMind.Healthcare.sln
```

```powershell
cd OpenMind.Healthcare\backend\DietApi
dotnet ef migrations add AddExerciseShortcuts -o Infrastructure/Data/Migrations
```

Migrations apply at startup. There is no seed data — shortcuts are member-created — so the
idempotency question does not arise here.

> If a build fails with `MSB3021 … file is locked`, a service is still running. Stop it and rebuild.

## Run

```powershell
dotnet run --project OpenMind.Healthcare\backend\UserApi     # 3004
dotnet run --project OpenMind.Healthcare\backend\DietApi     # 3005
cd OpenMind.Healthcare\frontend; npm start                   # 4200
```

New endpoints appear in Scalar at <http://localhost:3005/scalar/v1>.

## Validation scenarios

### V1 — Save one, tap it (US1)

1. Open **Diet → Today**. Record 45 minutes of running the usual way.
2. On that session, choose **save as a shortcut**. It appears above the search box with a readable
   default name.
3. Move to another day. Tap the shortcut. A 45 minute run is recorded with an estimate, and you were
   asked for nothing.
4. Tap it again on the same day. **Both sessions are kept** and the day's total is their sum.
5. Compare a session recorded by tapping against one entered by hand: same activity, duration and
   estimate, and both edit and delete the same way.

**Passes when**: step 3 asks for no input and step 5 finds no difference. *FR-001, FR-008 to FR-011,
SC-001, SC-002, SC-004*

### V2 — The estimate is computed now, not then (US1)

This is the scenario the design exists to protect. Run it carefully.

1. Save a shortcut and note the estimate on a session recorded from it.
2. Record a **new, lower body weight** on the Weight page.
3. Tap the same shortcut again. The new session's estimate is **lower** — it used your current
   weight, not the weight you had when you saved the button.
4. Confirm the session recorded in step 1 is **unchanged**. Its snapshot is its own.

**Passes when**: step 3 changes and step 1 does not. *FR-010, SC-003*

### V3 — The rules are not relaxed (US1)

1. Open a date before your plan started. The shortcut row is shown as unavailable, with the reason,
   and cannot be tapped.
2. Open a future date. Same.
3. Open today in two tabs. Tap a shortcut in tab A, then in tab B without reloading. Tab B is
   refused with **409**, and **tab A's session is still there**.
4. Sign out and call the shortcut endpoints with no token. 401 on every one.

**Passes when**: step 1 prevents the tap rather than failing it, and step 3 loses nothing.
*FR-012, FR-013, FR-018, SC-010, SC-011*

### V4 — Keep the list useful (US2)

1. Rename a shortcut. The new name shows everywhere and survives a reload.
2. Reorder: move one to the front. The order survives a reload.
3. Delete one. Open days where you had used it — **the sessions are untouched**.
4. Try to save a shortcut with the same activity and duration as an existing one. It is refused and
   the existing one is named.
5. Save shortcuts until you reach ten, then try one more. You are told the limit and invited to
   remove one.
6. Delete every shortcut. The panel explains how to make one rather than showing an empty row.

**Passes when**: step 3 leaves history alone and steps 4 and 5 refuse with a reason.
*FR-006, FR-007, FR-014 to FR-017, SC-006 to SC-009*

### V5 — Build one from scratch (US3)

1. Create a shortcut directly, choosing an activity you have never logged and entering a duration.
2. Tap it. The session records correctly.
3. Try to create one with a duration of 0, then one over the ceiling. Both are refused, with the
   same wording a session would give.

**Passes when**: step 3 matches the session rules exactly. *FR-002, FR-005, SC-005*

### V6 — The guarantees carried forward

1. Note a day's calorie target, consumed total and eating verdict. Record exercise on it **by
   shortcut**. Re-read the day: all three unchanged.
2. Confirm no screen or response combines the estimate with a calorie target.
3. Confirm no shortcut was created, changed or removed that you did not ask for.

**Passes when**: all three hold. *FR-019, FR-020, SC-012*

## Definition of done

- [ ] `dotnet build OpenMind.Healthcare.sln` — no new warnings
- [ ] `dotnet test OpenMind.Healthcare.sln` — passes, including the by-hand/by-shortcut match test
- [ ] `npm run build` in `frontend/` — succeeds, with **no new dependency** in `package.json`
- [ ] New endpoints appear in Scalar and require authorization
- [ ] A migration exists; an empty database yields a working schema
- [ ] V1 to V6 pass, **V2 especially**
- [ ] Constitution compliance re-checked
