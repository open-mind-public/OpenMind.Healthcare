# REST API Contract: Exercise Shortcuts

**Feature**: [../spec.md](../spec.md) | **Data model**: [../data-model.md](../data-model.md)

Added to `DietApi`. Conventions inherited unchanged from the diet contract
([001 contracts/README.md](../../001-diet-tracking/contracts/README.md)) — bearer auth on every
route, identity read only from the token, `DomainException` → 400, missing plan → 404, stale
`version` → 409, enums as names, `DateOnly` as `"2026-09-03"`.

The browser reaches these through the existing `/diet-api` prefix. **No new frontend prefix, compose
service or port.**

| Group | Base path | Feature folder |
|--------|--------|--------|
| Shortcuts | `/api/exercise-shortcuts` | `Features/ExerciseShortcuts` |
| Recording from one | `/api/exercise/{date}/entries/from-shortcut` | `Features/Exercise/AddEntryFromShortcut` |

All routes return **404** when the member has no plan.

## The shortcut shape

```json
{
  "id": "…",
  "name": "Morning run",
  "activityTypeId": "…",
  "activityName": "Running, 8 km/h",
  "durationMinutes": 45,
  "position": 0,
  "available": true
}
```

- `activityName` is resolved from the catalogue on read, not stored. A corrected activity name shows
  up here, which is right: this is a button, not a record (research R-003).
- `available` is false when the shortcut's activity is no longer in the catalogue. The client shows
  it as unusable rather than letting a tap fail (FR-013).
- **No estimate, no MET and no date.** The figure is computed when the session is recorded, from the
  member's current weight — never from anything held here (FR-010).

---

## `GET /api/exercise-shortcuts`

```json
{ "shortcuts": [ { "…": "as above" } ], "maxShortcuts": 10, "remainingSlots": 8 }
```

Ordered by the member's chosen position. `remainingSlots` lets the client say how many more may be
added before the limit, rather than discovering it on a failed save (FR-007).

*US1, US2 · FR-007, FR-015*

### `POST /api/exercise-shortcuts`

Request: `{ "activityTypeId", "durationMinutes", "name" }` — `name` optional; omitted, a readable
default is derived from the activity and duration (FR-004).

Returns the full list, so the client updates in one round trip. **400** on a duration the programme
would refuse on a session, on a duplicate (naming the shortcut it matches), or at the limit.
**404** when the activity is not in the catalogue.

*US1, US3 · FR-001 to FR-007*

### `PUT /api/exercise-shortcuts/{id}`

Request: `{ "name" }`. Returns the full list. **404** if the shortcut is not the caller's.

Renaming cannot create a duplicate — duplicates compare activity and duration only (research R-007).

*US2 · FR-014*

### `PUT /api/exercise-shortcuts/order`

Request: `{ "orderedIds": ["…", "…"] }` — **the complete list**, not a move.

Returns the full list. **400** when the ids are not exactly the member's current shortcuts.

> A full-list reorder is idempotent and race-free: two clients sending different orders produce one
> of the two, never an interleaving. Move-up and move-down against stale positions produce orders
> neither client asked for (research R-004).

*US2 · FR-015*

### `DELETE /api/exercise-shortcuts/{id}`

Returns the full list, with positions re-normalised so no hole is left. **404** if not the caller's.

Removes a button, never a session: everything recorded from it is untouched (FR-017, SC-009).

*US2 · FR-016, FR-017*

---

## `POST /api/exercise/{date}/entries/from-shortcut`

The one tap.

Request: `{ "shortcutId", "version" }` — `version` omitted when the date has no exercise day yet,
required otherwise, exactly as `POST /api/exercise/{date}/entries`.

Returns the full exercise day with a new `version`, the same shape that endpoint returns.

- **400** on a future date, a date before the plan started, or a duration the rules refuse.
- **404** when the shortcut is not the caller's, or its activity is no longer in the catalogue.
- **409** on a stale `version`.

The session recorded is **indistinguishable** from one entered by hand: same activity, duration,
snapshotted name and MET, and an estimate computed from the member's current weight at this moment
(FR-009, SC-002). Both paths end in the same aggregate method (research R-005).

*US1 · FR-008 to FR-013*

---

## What this contract deliberately does not do

| Not present | Why |
|--------|--------|
| No estimate, MET or activity name stored on a shortcut | FR-010 — a saved button must not freeze the member's weight at the moment they saved it |
| No duration override on the tap | Scope — a "but today it was 50 minutes" step is no longer one tap; the session is editable afterwards |
| No endpoint that creates a shortcut from anything but a member's request | FR-020 — nothing is suggested or inferred in this release |
| No shortcut field on any `/api/exercise` response for a recorded session | A session records what happened, not which button produced it |
| No relaxation of the date, duration or concurrency rules on the tap | FR-012 — a shortcut is a faster path to the same behaviour, not a different one |

## Coverage

| User story | Endpoints |
|--------|--------|
| US1 Record in one tap | `GET /api/exercise-shortcuts`, `POST /api/exercise-shortcuts`, `POST /api/exercise/{date}/entries/from-shortcut` |
| US2 Keep the list useful | `PUT /api/exercise-shortcuts/{id}`, `PUT /api/exercise-shortcuts/order`, `DELETE /api/exercise-shortcuts/{id}` |
| US3 Build one without logging first | `POST /api/exercise-shortcuts` |
