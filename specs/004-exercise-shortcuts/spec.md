# Feature Specification: Exercise Shortcuts

**Feature Branch**: `004-exercise-shortcuts`

**Created**: 2026-09-03

**Status**: Draft

**Input**: User description: "A member who does the same exercise most days wants to record it in one click. They can define a shortcut — an activity plus a duration, saved explicitly by the member (typically from a session they just logged) — and tapping it records that session against today with no further input. Shortcuts are the member's own, curated by them: they can be created, renamed, reordered or removed. Suggesting shortcuts automatically from history is out of scope for this release; shortcuts are always saved deliberately by the member."

## Overview

Recording exercise currently takes four interactions: type into the search box, wait for matches,
pick the activity, type a duration, confirm. That is a reasonable price for a session a member
records once. It is an unreasonable one for the run they do every morning, and the friction is
paid on exactly the days a habit is most fragile.

A shortcut is a named activity and duration the member has saved. Tapping it records that session
with no further input. Nothing is inferred and nothing is automatic: a shortcut exists because the
member chose to save it, and it does what it says every time.

## Scope

In scope: saving a shortcut, tapping one to record a session, and keeping the list useful —
renaming, reordering and removing.

Deliberately out of scope for this release:

- **Shortcuts suggested from history.** The programme could notice a repeated activity and offer it
  unprompted. That is a different feature with its own thresholds, and it would make the list
  something the member manages rather than owns.
- **Adjusting the duration as you tap.** A shortcut with a "but today it was 50 minutes" step is no
  longer one interaction. A member who ran longer than usual edits the session afterwards, which
  the exercise log already allows.
- **Sharing shortcuts between members.** Shortcuts are personal.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record a repeated session in one tap (Priority: P1)

A member runs most mornings. Having recorded one run the usual way, they save it as a shortcut.
From then on, recording the same run is a single tap: the session appears on the day with its
estimate, exactly as if they had typed it.

**Why this priority**: It is the entire point of the feature, and it is the whole loop — saving and
tapping are worthless apart. Everything else here refines a list that this story creates.

**Independent Test**: Record a 45 minute run, save it as a shortcut, then on another day tap the
shortcut and confirm a 45 minute run is recorded with an estimate, indistinguishable from one
entered by hand.

**Acceptance Scenarios**:

1. **Given** a member viewing a day with a session they have just recorded, **When** they save that
   session as a shortcut, **Then** it appears in their shortcuts with a name they can recognise.
2. **Given** a member with a saved shortcut, **When** they tap it, **Then** a session with that
   activity and duration is recorded against the day they are viewing, with an energy estimate, and
   no further input is asked of them.
3. **Given** a member taps a shortcut on a day that already has that same session, **When** the tap
   is recorded, **Then** both sessions are kept and the day's total is their sum — a shortcut adds,
   it never replaces.
4. **Given** a member has changed weight since saving the shortcut, **When** they tap it, **Then**
   the estimate reflects their current weight, not the weight they had when the shortcut was saved.
5. **Given** a member is viewing a date they cannot record against, such as one before their plan
   started, **When** they look at their shortcuts, **Then** tapping is unavailable and the reason is
   given, rather than a tap failing.

---

### User Story 2 - Keep the list worth having (Priority: P2)

The member's shortcuts accumulate. They rename one to something they actually recognise, drag the
one they use daily to the front, and delete the one they saved for a class they no longer attend.

**Why this priority**: A list that only grows stops being a shortcut. It ranks below US1 because a
member with two shortcuts does not need to manage them, and one with none has nothing to manage.

**Independent Test**: With three shortcuts saved, rename one, move another to the front, delete the
third, reload, and confirm all three changes survived.

**Acceptance Scenarios**:

1. **Given** a member with shortcuts, **When** they rename one, **Then** the new name is shown
   everywhere the shortcut appears and survives a reload.
2. **Given** a member with several shortcuts, **When** they reorder them, **Then** the order they
   chose is the order they see next time.
3. **Given** a member deletes a shortcut, **When** they look at days where they had used it,
   **Then** the sessions recorded from it are untouched — deleting a shortcut removes a button, not
   a member's history.
4. **Given** a member has reached the limit on how many shortcuts they may keep, **When** they try
   to save another, **Then** they are told the limit and invited to remove one, rather than the
   save silently failing.

---

### User Story 3 - Build one without logging it first (Priority: P3)

A member knows they will start swimming on Tuesdays. Rather than waiting to log a swim before they
can save a shortcut for it, they create one directly by choosing the activity and the duration.

**Why this priority**: It removes an ordering constraint that would otherwise be arbitrary, but the
common path is saving what you just did. It is worth building only once that path works.

**Independent Test**: Create a shortcut from scratch for an activity never logged before, tap it,
and confirm the session records correctly.

**Acceptance Scenarios**:

1. **Given** a member creating a shortcut directly, **When** they choose an activity from the
   catalogue and enter a duration, **Then** the shortcut is saved and behaves identically to one
   saved from a logged session.
2. **Given** a member entering a duration that the programme would refuse on a session, **When**
   they try to save the shortcut, **Then** it is refused for the same reason and with the same
   wording — a shortcut cannot hold a session that could never be recorded.

---

### Edge Cases

- What happens when a member taps the same shortcut twice in quick succession? Two sessions are
  recorded, because that is what two taps mean and it is what the programme already does for two
  entries. The member can remove one.
- What happens when a member tries to save a shortcut identical to one they already have? They are
  told it already exists and no duplicate is created — two buttons that do the same thing make the
  list worse, not better.
- What happens when a member saves a shortcut, and the activity is later corrected in the
  catalogue? The shortcut still points at that activity, and sessions recorded from it afterwards
  use the corrected figures. Sessions already recorded are untouched.
- What happens when a shortcut's activity is no longer available at all? The shortcut is shown as
  unusable with the reason, and tapping it records nothing.
- What happens when a member has no shortcuts yet? They are shown how to make one, not an empty
  panel.
- What happens when a member taps a shortcut while the day has been changed in another tab? The tap
  is refused as a stale write, exactly as any other change to that day would be, and the member is
  asked to reload.
- What happens when a member deletes every shortcut? The panel returns to explaining how to make
  one.
- What happens when a member's plan is removed or missing? Shortcuts are unavailable, as everything
  else in the programme is without a plan.

## Requirements *(mandatory)*

### Functional Requirements

#### Saving a shortcut

- **FR-001**: Members MUST be able to save a shortcut from a session they have recorded, in one
  action from where that session is shown.
- **FR-002**: Members MUST be able to create a shortcut directly by choosing an activity from the
  catalogue and entering a duration, without having recorded that session first.
- **FR-003**: A shortcut MUST hold exactly one activity and one whole-minute duration. It holds
  nothing else about how the session will be recorded.
- **FR-004**: System MUST give every new shortcut a readable default name derived from its activity
  and duration, so a member never has to name one to use it.
- **FR-005**: System MUST refuse a duration on a shortcut that it would refuse on a session, with
  the same reason.
- **FR-006**: System MUST refuse to save a shortcut identical in activity and duration to one the
  member already has, and MUST say which existing shortcut it matches.
- **FR-007**: System MUST limit how many shortcuts a member may keep, and MUST state the limit and
  invite the member to remove one when it is reached.

#### Using a shortcut

- **FR-008**: Tapping a shortcut MUST record a session with that activity and duration against the
  day the member is viewing, asking for no further input.
- **FR-009**: A session recorded from a shortcut MUST be identical in every respect to the same
  session entered by hand, and MUST be editable and removable in the same ways.
- **FR-010**: The energy estimate MUST be computed at the moment of recording, from the activity's
  current energy rate and the member's current weight — never from figures held on the shortcut.
- **FR-011**: Tapping a shortcut MUST add a session; it MUST NOT replace or amend any session
  already recorded on that day.
- **FR-012**: System MUST apply every rule that already governs recording a session — the date
  cannot be in the future or before the plan started, and the day's version must be current —
  and MUST refuse the tap with the same reasons when they are broken.
- **FR-013**: When a shortcut cannot be used on the day in view, System MUST show it as unavailable
  with the reason, rather than letting a tap fail.

#### Keeping the list useful

- **FR-014**: Members MUST be able to rename a shortcut.
- **FR-015**: Members MUST be able to choose the order their shortcuts appear in, and that order
  MUST persist.
- **FR-016**: Members MUST be able to remove a shortcut.
- **FR-017**: Removing a shortcut MUST NOT alter, remove or re-estimate any session already
  recorded from it.

#### Boundaries and access

- **FR-018**: Shortcuts MUST belong to one member. A member MUST NOT be able to see, use or change
  another member's shortcuts, and unauthenticated requests MUST be refused.
- **FR-019**: A shortcut MUST NOT change a member's calorie target, their declared activity level,
  or any day's eating assessment — recording by shortcut is recording, and carries every guarantee
  that recording already carries.
- **FR-020**: System MUST NOT create, alter or remove a shortcut except when the member asks. No
  shortcut is suggested, inferred from history, or added automatically in this release.

### Key Entities *(include if data involved)*

- **Shortcut**: One saved way to record a session — its name, the activity it points at, the
  duration it records, where it sits in the member's order, and who it belongs to. It points at a
  catalogue activity rather than copying it, so a corrected energy rate reaches sessions recorded
  from it in future.
- **Shortcut list**: A member's shortcuts in the order they chose, and how many more they may add.

## Success Criteria *(mandatory)*

### Release gates

- **SC-001**: A member with a saved shortcut records a repeat session in a single interaction,
  against at least four without one.
- **SC-002**: A session recorded from a shortcut is indistinguishable from the same session entered
  by hand — same activity, duration, estimate and behaviour on edit and delete.
- **SC-003**: The estimate on a session recorded from a shortcut reflects the member's weight and
  the activity's energy rate at the moment of recording, verified by changing both after the
  shortcut was saved.
- **SC-004**: Saving a session as a shortcut takes one interaction from where the session is shown.
- **SC-005**: A duration refused on a session is refused on a shortcut, for every rule that governs
  duration.
- **SC-006**: Attempting to save a duplicate shortcut creates nothing and names the existing one.
- **SC-007**: Reaching the shortcut limit produces a message stating the limit, not a silent
  failure.
- **SC-008**: Renaming, reordering and removing all survive a reload.
- **SC-009**: Removing a shortcut leaves every session recorded from it byte-for-byte unchanged.
- **SC-010**: A member's shortcuts are never visible or usable by another member, and
  unauthenticated requests are refused.
- **SC-011**: Tapping a shortcut on a day that cannot accept the session is prevented before the
  tap, with the reason shown.
- **SC-012**: No shortcut is ever created, changed or removed without the member asking.

### Post-launch measures

- **SC-013**: Among members who save at least one shortcut, the share of exercise sessions recorded
  by tapping one exceeds half within a month.
- **SC-014**: Members who save a shortcut record exercise on at least as many days in the following
  month as they did in the previous one.

## Assumptions

- A shortcut records against **the day the member is viewing**, not always today. Viewing today —
  the common case — gives exactly the one-tap behaviour asked for, and the same button then works
  when catching up on yesterday. Every date rule that governs recording still applies.
- Ten shortcuts is the limit. Beyond roughly that many, scanning the list costs more than typing
  the session, and the feature stops being a shortcut.
- A shortcut points at a catalogue activity rather than copying its energy rate. Copying would
  freeze a figure that the catalogue may later correct, and new sessions should use the best
  current figure. Sessions already recorded keep their own snapshot, as they do today.
- Tapping records immediately with no confirmation step. A confirmation would defeat the purpose;
  an accidental session is removed the same way any other is.
- Shortcuts belong to the diet programme's exercise log. The quit-smoking programme is untouched.
- A member must have a plan to have shortcuts, as with everything else in the programme.
- Shortcuts hold no estimate, no date and no history. They are a way to record, not a record.

## Dependencies

- The existing exercise log supplies the recording behaviour, the rules and the estimate. This
  feature adds a faster way to reach it and changes none of it.
- The activity catalogue supplies the activities a shortcut can point at.
- The member's plan supplies the start date that bounds recording and the weight the estimate uses.
- Platform authentication identifies the member; this feature introduces no separate access model.
