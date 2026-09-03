# Phase 0 Research: Exercise Shortcuts

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

Every finding below was checked against the running codebase rather than assumed.

---

## R-001: Where this feature lives

**Decision**: Additive inside `DietApi`. A new `Features/ExerciseShortcuts/` slice, one new owned
collection on the existing `DietPlan` aggregate, one migration, and changes to the existing exercise
log screen. No new service, port, container, volume or frontend prefix.

**Rationale**: Shortcuts are a member's own settings for recording exercise, which is already the
diet context's business. Constitution Principle I is satisfied by staying inside the context that
owns the data.

---

## R-002: Shortcuts are owned by `DietPlan`, not their own aggregate

**Decision**: `ExerciseShortcut` is an owned entity on the `DietPlan` aggregate, alongside
`WeightReadings` and `UnlockedAchievements`.

**This reverses an earlier off-hand judgement.** Before the spec existed it looked as though
shortcuts should follow `ExerciseDay` and become their own aggregate root. Writing the requirements
down showed the opposite, and the reason is worth recording because it is the whole argument for
where an aggregate boundary belongs.

**Rationale**: Two requirements are invariants over the *set* of a member's shortcuts, not over any
one of them:

- **FR-006** — no two shortcuts may hold the same activity and duration.
- **FR-007** — a member may keep at most ten.

Neither can be enforced by an aggregate that contains a single shortcut. Checking "do I already
have this one?" and then saving would be a read-modify-write across aggregate boundaries, and two
concurrent saves would both pass the check. An invariant that spans a set needs a consistency
boundary that contains the set, which is precisely what an aggregate is for.

The usual counter-argument — that owning a collection makes writes scale with its size (ADR 0001) —
does not apply, because FR-007 caps the collection at ten. That cap is what makes ownership safe,
and it is a requirement rather than a convenience.

**Alternatives considered**:

- **Its own aggregate root**, like `ExerciseDay`. Rejected: it cannot enforce FR-006 or FR-007
  without a race, and it would need its own repository and concurrency token for a list that is
  never larger than ten rows.
- **No persistence, held in the browser.** Rejected: shortcuts would not survive a new device, and
  a member's own settings belong with their plan.

---

## R-003: A shortcut points at an activity, it does not copy one

**Decision**: A shortcut stores `ActivityTypeId` and a duration. It stores **no** activity name, no
MET value and no energy estimate.

**Rationale**: FR-010. The estimate has to be computed at the moment of recording, from the
activity's current energy rate and the member's *current* weight. Caching an estimate on the
shortcut is the obvious optimisation, and it would freeze a member's weight at the moment they
saved the button — a member who lost ten kilograms would go on getting estimates for the person
they used to be, from a control that gives no hint it is stale.

This is the mirror image of the snapshotting rule elsewhere in the programme, and the distinction
matters:

| Thing | Behaviour | Why |
|--------|--------|--------|
| A **recorded session** | Snapshots the name, MET and estimate at the moment it is recorded | A figure the member saw and acted on must not be rewritten later (002 FR-009) |
| A **shortcut** | References the activity, snapshots nothing | It is an instruction to record in future, so it should use the best figure available *then* |

**Consequence worth stating**: a shortcut's displayed name is derived from the catalogue on read,
unless the member renamed it. A corrected activity name therefore shows up on the shortcut, which
is correct — it is a button, not a record.

**Alternatives considered**: storing a denormalised name and MET for display speed. Rejected: it
buys nothing at ten rows and creates two places for the truth to live.

---

## R-004: Ordering is an explicit position, normalised by the aggregate

**Decision**: Each shortcut carries an integer `Position`. The aggregate keeps positions contiguous
from zero and is the only thing that assigns them. Reordering is expressed as **the full ordered
list of ids**, not as "move this one up".

**Rationale**: FR-015. A full-list reorder is idempotent and has no race: two clients sending
different orders produce one of the two orders, never an interleaving. Move-up and move-down
operations against stale positions produce orders neither client asked for.

Normalising inside the aggregate means a deleted shortcut cannot leave a hole, and no caller can
create two shortcuts at position 3.

**Alternatives considered**: a fractional or sparse ordering key, which avoids rewriting every row
on reorder. Rejected as premature at ten rows.

---

## R-005: Tapping a shortcut goes through the same domain path as typing one

**Decision**: A dedicated endpoint records from a shortcut. It resolves the shortcut, then performs
exactly the same aggregate operations as recording by hand: load or create the `ExerciseDay`, check
the same rules, call the same `AddEntry`, take the same estimate from the member's current weight.

**Rationale**: SC-002 requires that a session recorded from a shortcut be indistinguishable from one
entered by hand. The safest way to guarantee that is for both paths to end in the same domain call,
and for a test to record the same session both ways and compare the results field by field.

**Alternatives considered**:

- **The client resolves the shortcut and calls the existing add endpoint.** Tempting, and it makes
  SC-002 true by construction. Rejected because it puts the resolution in the least trustworthy
  place: a client that drifts, or a second client that never gets written, silently breaks the
  guarantee. It is also two round trips for something sold as one tap.
- **The shortcut handler dispatches the existing add command through the mediator.** Rejected:
  handler-calling-handler hides the call graph and makes failures harder to read. The shared code
  is one aggregate method, and both handlers calling it directly is clearer than indirection.

---

## R-006: The tap obeys every rule recording already obeys

**Decision**: No rule is relaxed for a shortcut. Future dates, dates before the plan started, a
stale day version and duration bounds all behave exactly as they do for a typed session.

**Rationale**: FR-012. A shortcut is a faster way to reach the same behaviour, not a different
behaviour. In particular the day's concurrency token is still required and a stale one still gives
a conflict — a shortcut tapped in one tab must not silently overwrite a change made in another.

**Consequence**: FR-013 asks that a shortcut be shown as unavailable rather than failing on tap.
The client already knows the date it is showing and the plan's start date, so it can disable the
panel without a round trip. The server still enforces the rules; the client just does not offer a
tap it knows will be refused.

---

## R-007: Duplicate detection compares activity and duration, not name

**Decision**: Two shortcuts are duplicates when they hold the same activity **and** the same
duration. The name is not part of the comparison.

**Rationale**: FR-006 exists to stop two buttons that do the same thing. Two buttons named
differently that both record a 45 minute run are exactly that problem; two buttons for a 30 minute
and a 45 minute run are not duplicates however similarly they are named.

**Consequence**: renaming can never create a duplicate, so FR-014 needs no uniqueness check.

---

## R-008: One migration, one table

**Decision**: A single migration adding an `ExerciseShortcuts` table, owned by `DietPlan` and keyed
by `(DietPlanId, Id)`, with a unique index on `(DietPlanId, ActivityTypeId, DurationMinutes)` to
back FR-006 and a supporting index on `(DietPlanId, Position)`.

**Rationale**: Principle VI. The unique index makes the duplicate rule true at the storage layer as
well as in the domain, so a race that slipped past the aggregate still cannot land.

**No seed data.** Shortcuts are member-created; there is nothing to seed and nothing to make
idempotent.

---

## R-009: Front end

**Decision**: Three changes to the existing exercise log, and no new page:

1. A row of shortcut buttons above the "Add a session" search.
2. A "save as a shortcut" action on each recorded session (FR-001).
3. A small manage view — rename, reorder, remove — reached from the shortcut row.

**Rationale**: The shortcuts belong where the recording happens; a separate settings page would put
them a navigation away from the moment they are useful. No new programme nav entry, no new route.

**No new dependency.** Reordering is drag-free: up and down controls in the manage view, which
send the full ordered list (R-004). A drag-and-drop library would be the only new package in the
front end and buys little for a list of ten.

---

## R-010: The guarantees carried forward

**Decision**: Recording by shortcut changes nothing about eating. No calorie target moves, no day's
assessment changes, and no figure combines the estimate with intake.

**Rationale**: FR-019, carried unchanged from 002 and 003. A shortcut is a recording path, so it
inherits the guarantee rather than needing a new one — but the tests that assert it should cover the
shortcut path too, because "we added a second way to record" is exactly how a guarantee that was
only tested on the first path gets lost.

---

## Open questions carried into implementation

None blocking. One thing to confirm while building: that the unique index on
`(DietPlanId, ActivityTypeId, DurationMinutes)` survives EF Core's owned-entity configuration — the
existing `WeightReadings` mapping already carries a unique index on `(DietPlanId, Date)`, so the
pattern is proven, but it is worth seeing the generated migration rather than assuming.
