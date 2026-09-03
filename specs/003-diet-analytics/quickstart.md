# Quickstart: Validating Diet Analytics

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Contract**: [contracts/rest-api.md](./contracts/rest-api.md)

How to run the feature and prove it works. Request and response shapes are in the contract; field
rules are in [data-model.md](./data-model.md). Nothing is duplicated here.

## Prerequisites

Unchanged from the diet programme — this feature adds no service, port or container, and **no
migration**. See the [repository README](../../README.md) for ports.

- .NET 10 SDK, Node 20+
- A member account with a **diet plan and at least a month of logged days**. Analytics over three
  days proves nothing; several of the scenarios below need fourteen or more.

## Build, test, run

```powershell
dotnet build OpenMind.Healthcare.sln
dotnet test OpenMind.Healthcare.sln
```

There is no `dotnet ef migrations add` step. If one seems necessary, something has been added to
the model that this feature was not supposed to add — check before writing it.

```powershell
dotnet run --project OpenMind.Healthcare\backend\UserApi     # 3004 - issues the token
dotnet run --project OpenMind.Healthcare\backend\DietApi     # 3005
cd OpenMind.Healthcare\frontend; npm start                   # 4200
```

New endpoints appear in Scalar at <http://localhost:3005/scalar/v1>.

> If a build fails with `MSB3021 … file is locked`, a service is still running. Stop it and rebuild.

## Seeding history worth analysing

Several scenarios need a deliberate shape, not just volume. Seed a member with:

- **~30 logged days**, most over target, a few on target, and 5–6 days not logged at all
- **one food appearing on most days** — enough to clear the 15% dominance threshold
- **heavier Saturdays and Sundays** than weekdays, by roughly 25%
- **a third of entries timestamped after 21:00** in the member's local time
- **a calorie target change** part-way through the period
- **weight readings on some days only**, so gaps are exercised

The three-year scale seed used by `ThreeYearHistoryTests` is the starting point for V6.

## Validation scenarios

Each proves one user story end to end. Run in order.

### V1 — Where the calories go (US1)

1. Open **Diet → Analytics** for the last month. Total and average daily intake are shown, and the
   average states how many days it is drawn from.
2. Add the four meal figures together. They equal the reported total exactly.
3. Add the category figures together. Same total.
4. Confirm the top-foods list does **not** claim to sum to the total — it is a top ten, not a
   partition.
5. Add on-target, over-target and not-logged days. They equal the number of calendar days in the
   period, not the number logged.
6. Switch to the last week, then the whole plan. Every figure changes and every denominator
   changes with it.

**Passes when**: steps 2, 3 and 5 reconcile exactly, and step 1 never shows an average without its
denominator. *FR-003, FR-005 to FR-009, SC-002, SC-003*

### V2 — Targets and macronutrients (US2)

1. Open the macronutrient section. Protein, carbohydrate and fat are shown against the targets in
   force, as amounts and as shares of energy.
2. **The one that matters**: pick a period spanning your target change. Compute by hand what the
   average of each day's own stored target should be, and confirm the reported target matches —
   not the plan's current target.
3. On a plan with no macronutrient targets, confirm the split still appears and nothing is
   compared against an invented target.

**Passes when**: step 2 matches the hand calculation. *FR-010 to FR-012, SC-006*

### V3 — When I eat (US3)

1. Open the patterns section. Seven weekdays and twenty-four hours are shown.
2. Confirm the screen states that times are when an entry was recorded, not when it was eaten.
3. Change your machine's timezone to one with a half-hour offset (+05:30) and reload. The hourly
   distribution shifts by exactly five and a half hours — **not** by five or six.
4. Confirm a weekday with nothing logged reads as zero with no logged days, not as missing.

**Passes when**: step 3 lands exactly and step 2 is present. *FR-013 to FR-015*

### V4 — What was noticed (US4)

1. Open analytics for a member with the seeded patterns. Observations appear, strongest first, each
   with its figure.
2. Confirm no two observations describe the same underlying pattern.
3. Reload. The identical list appears, in the identical order.
4. Repeat with a member who has **nine** logged days. No observation appears, and the screen says
   more days are needed rather than showing a blank.
5. Read every observation the system can produce. None diagnoses a condition, calls the member's
   eating good or bad, or tells them what to eat.

**Passes when**: step 3 is identical, step 4 stays silent, and step 5 survives a careful read.
*FR-016 to FR-022, SC-008, SC-009, SC-010*

### V5 — The guarantee, and the boundaries

1. Search every analytics response for a field combining exercise energy with intake or a target.
   There must be none — no `net`, no `available`, no `burned` offset against `consumed`.
2. Note a logged day's target, consumed total and state. View analytics over a period containing
   it. Re-read the day: all three are unchanged.
3. Sign out and call each of the four endpoints with no token. All four return 401.
4. Delete a food entry, then reopen analytics. The figures reflect the deletion immediately.
5. Confirm no analytics route accepts anything but `GET`.

**Passes when**: all five hold. *FR-023 to FR-027, SC-007, SC-011, SC-012*

### V6 — Scale and honesty

1. Seed three years of daily logging and open each of the four sections. Each returns in under two
   seconds. If not, the query shapes in research R-008 are what to revisit — not the criterion.
2. Confirm no query loads food entries into memory to total them; the aggregation happens in SQL.
3. With a brand-new plan and nothing logged, open analytics. A plain explanation and a next step,
   not an empty chart or an error.
4. Request the whole-plan period. The comparison reports as unavailable, **not** as zeros.

*SC-004, SC-005*

## Definition of done

- [ ] `dotnet build OpenMind.Healthcare.sln` — no new warnings
- [ ] `dotnet test OpenMind.Healthcare.sln` — passes, including the observation-minimums test and
      the structural no-combined-figure test
- [ ] `npm run build` in `frontend/` — succeeds, with **no new dependency** in `package.json`
- [ ] New endpoints appear in Scalar and require authorization
- [ ] **No migration was added** — the model is unchanged
- [ ] V1 to V6 pass, **V2 step 2 and V4 steps 3–5 especially**
- [ ] Every observation's wording reviewed against FR-019 before release
- [ ] Constitution compliance re-checked, and ADR 0004 written up for the read-model repository
