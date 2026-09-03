# Feature Specification: Exercise Logging

**Feature Branch**: `002-exercise-logging`

**Created**: 2026-09-03

**Status**: Draft

**Input**: User description: "I need ability to log excercise dates for diet"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record That I Exercised (Priority: P1)

A member who has been for a run opens the diet area and records it against today's date. They pick
what they did from a list of activities, say how long it lasted, and save. The day now shows that
they exercised.

**Why this priority**: This is the whole request. A member who can only do this — record activity
against a date — already gets the thing they asked for: a truthful record of the days they moved.
It is the smallest slice that can be built, demonstrated and shipped on its own.

**Independent Test**: Sign in with a plan in place, record an activity for today, reload the page,
and confirm it persists against the right date. Delivers a working activity record with no history
view, no summary and no effect on anything else.

**Acceptance Scenarios**:

1. **Given** a member with an active plan and nothing recorded today, **When** they view today, **Then** no exercise is shown and they are invited to add some.
2. **Given** a member selecting an activity and a duration, **When** they save, **Then** the entry is recorded against that date and appears immediately.
3. **Given** a member who exercised twice in one day, **When** they record a second session, **Then** both are kept as separate entries rather than one replacing the other.
4. **Given** a member who forgot to record yesterday, **When** they select that past date and record an activity, **Then** it is saved against that date.
5. **Given** a member selecting a date in the future, **When** they attempt to record an activity, **Then** it is rejected with a message explaining future dates cannot be logged.
6. **Given** a member selecting a date before their plan's start date, **When** they attempt to record an activity, **Then** it is rejected with a message explaining the date precedes the plan.
7. **Given** a member entering a duration of zero or less, **When** they attempt to save, **Then** it is rejected with an explanatory message.
8. **Given** a visitor who is not signed in, **When** they attempt to view or record exercise, **Then** access is refused.
9. **Given** a member attempting to view or modify an exercise entry belonging to another member, **When** the request is made, **Then** access is refused.
10. **Given** a member who records a 45-minute activity, **When** they view the day, **Then** an estimated energy figure is shown for it, plainly labelled as an estimate.
11. **Given** a day already assessed as over target for eating, **When** the member records exercise against it, **Then** the day's calorie target is unchanged and it is still over target.
12. **Given** a member viewing a day with exercise recorded, **When** they read the day's figures, **Then** the estimated energy used is presented separately and never added to the calories they may eat.

---

### User Story 2 - Correct What I Recorded (Priority: P2)

A member realises they logged 30 minutes when it was 45, or recorded a run they did not actually
do. They change the duration or remove the entry, and the record updates.

**Why this priority**: A log that cannot be corrected stops being trusted, and an untrusted log
stops being used. It depends on there being entries to correct, so it follows User Story 1.

**Independent Test**: Record two activities, edit one's duration and delete the other, and confirm
the day reflects both changes after a reload.

**Acceptance Scenarios**:

1. **Given** a member with a recorded activity, **When** they change its duration, **Then** the change is saved and shown immediately.
2. **Given** a member with a recorded activity, **When** they change which activity it was, **Then** the entry updates accordingly.
3. **Given** a member with a recorded activity, **When** they delete it, **Then** it disappears from that date.
4. **Given** a member who deletes their only activity for a date, **When** they view that date, **Then** it shows as a day with no exercise rather than an error or an empty shell.
5. **Given** the same day open on two devices, **When** the second device saves a change based on a stale copy, **Then** the second save is refused with a message asking the member to reload, and no entry from either device is lost.

---

### User Story 3 - See Exercise Alongside My Eating (Priority: P3)

A member looks back over the month and sees, on the same calendar they already use for eating,
which days they moved. Days with exercise are marked distinctly from days without, so patterns —
"I eat better on days I train" — become visible without cross-referencing two screens.

**Why this priority**: Recording activity is only worth doing if the member can see it back. It
depends on accumulated entries, so it follows the logging itself.

**Independent Test**: Seed a range of dates with and without activity, open the calendar for the
covering month, and confirm the exercise marking matches the seeded data and sits alongside the
existing eating marking without replacing it.

**Acceptance Scenarios**:

1. **Given** a member with exercise on some days and not others, **When** they open the calendar, **Then** days with exercise carry a distinct marking.
2. **Given** a day that is both on target for eating and has exercise recorded, **When** they view the calendar, **Then** both facts are visible; neither hides the other.
3. **Given** a member selecting a day from the calendar, **When** the day opens, **Then** the activities recorded that day are listed with their durations.
4. **Given** a member with no exercise recorded at all, **When** they open the calendar, **Then** an empty state is shown rather than an error.

---

### User Story 4 - See How Active I Have Been (Priority: P4)

A member wants a sense of their week: how many days they moved, how long in total, and how that
compares with the weeks before. Not a training plan — just an honest count.

**Why this priority**: A summary makes the record useful at a glance, but nothing depends on it and
it is meaningless until there is history. It ships last.

**Independent Test**: Seed several weeks of activity, open the summary, and confirm the active-day
count, total duration and recent trend match the seeded data.

**Acceptance Scenarios**:

1. **Given** a member with activity across the week, **When** they view the summary, **Then** the number of active days and total time this week are shown.
2. **Given** a member with several weeks of history, **When** they view the summary, **Then** they can see how the current week compares with recent ones.
3. **Given** a member with no activity this week, **When** they view the summary, **Then** zero is shown plainly rather than an error or a blank panel.
4. **Given** a member who has recorded exercise consistently, **When** they view their plan, **Then** their daily target is unchanged by that record, and the reason is explained where the two appear together.

---

### Edge Cases

- **Exercise on a day with no food logged**: Recording activity must not, on its own, make a day count as logged for eating. The two records are separate, and a day with a run but no meals is still an unlogged eating day.
- **Several sessions in one day**: A morning swim and an evening walk are two entries, both kept, and any daily total is their sum.
- **Day boundaries**: An activity recorded late at night, or by a member who has travelled, must belong to an unambiguous calendar day that does not shift afterwards.
- **Dates outside the plan**: Days before the plan start and days in the future accept no activity and are excluded from every count and average.
- **Implausible durations**: A single entry claiming an impossible duration must be rejected rather than silently distorting totals.
- **Deleting the last activity of a day**: The date returns to having no exercise; it must not linger as a zero-minute session.
- **Correcting a past entry**: Editing an entry recorded weeks ago updates that date, not today, and must not disturb how that day was judged for eating.
- **Logging exercise for a past day**: Recording Tuesday's run on Thursday adds it to Tuesday and changes nothing about how Tuesday was assessed. A day's verdict is settled by what was eaten against the target then in force.
- **An estimate without a weight**: The estimate depends on the member's current weight, which a plan always carries, so the figure is always computable.
- **A very light activity**: An estimate that rounds to nothing must be shown as a small number or omitted, never as a misleading zero next to a real session.
- **Concurrent edits**: The same day's activity edited from two devices at once must not lose entries or leave a stored total disagreeing with the entries beside it.
- **Very long histories**: A member with years of daily activity must still open a calendar or summary without noticeable delay.
- **A retired activity**: If an activity is removed from the catalogue later, entries already recorded against it must still display correctly.

## Requirements *(mandatory)*

### Functional Requirements

**Recording activity**

- **FR-001**: System MUST allow a signed-in member to record an exercise entry against a calendar date.
- **FR-002**: System MUST capture, for each entry, the date, the activity performed, and its duration in whole minutes. Intensity is carried by the catalogue rather than asked of the member — "Running, easy" and "Running, fast" are separate activities, which is how published energy tables already distinguish them.
- **FR-003**: Members MUST be able to identify what they did by choosing from a curated catalogue of activities maintained with the application.
- **FR-004**: System MUST allow more than one entry per date and MUST NOT treat a later entry as replacing an earlier one.
- **FR-005**: System MUST reject entries dated in the future and entries dated before the plan's start date.
- **FR-006**: System MUST reject entries whose duration is zero, negative, or beyond a plausible single-session ceiling.
- **FR-007**: System MUST record each entry against an unambiguous calendar day that does not change once recorded.
- **FR-008**: System MUST allow entries to be added to any past date on or after the plan start date, so a member can fill in days they missed.
- **FR-009**: System MUST preserve the activity name and any values captured at the time of recording, so later corrections to the catalogue do not retroactively alter a member's history.

**Correcting activity**

- **FR-010**: Members MUST be able to edit their own entries — the activity, the duration, and any other captured value — with totals reflecting the change immediately.
- **FR-011**: Members MUST be able to delete their own entries.
- **FR-012**: System MUST detect when a day's activity has been changed by another session since it was read, and MUST refuse the conflicting write with a message telling the member to reload, rather than silently discarding entries.

**Relationship to eating**

- **FR-013**: System MUST treat exercise and food as separate records: recording exercise MUST NOT by itself cause a date to count as a logged eating day, and deleting all food entries MUST NOT remove that date's exercise.
- **FR-014**: System MUST show an estimate of the energy used by a day's recorded exercise.
- **FR-015**: System MUST NOT add that estimate to the day's calorie target, and MUST NOT change whether the day counts as on target or over target. A day is judged on what was eaten against the target that was in force when it was logged, and recording exercise — including recording it days later — MUST leave that judgement untouched.
- **FR-016**: System MUST NOT present the estimate as additional calories available to eat, or combine it with the target into a single spendable figure.
- **FR-017**: System MUST derive the estimate from the activity's energy rate, the duration recorded, and the member's current weight, and MUST label it plainly as an estimate.
- **FR-018**: System MUST keep recorded exercise and the activity level declared in the plan independent: recorded exercise MUST NOT alter the plan's activity level, its daily target, or any suggested target.
- **FR-019**: System MUST explain, where exercise and target appear together, that the declared activity level already accounts for habitual exercise and that logged sessions are a record rather than an allowance.

**Seeing activity**

- **FR-020**: System MUST show, for any given date, the activities recorded that day with their durations, a daily total duration, and the estimated energy used.
- **FR-021**: System MUST mark days with recorded exercise distinctly in the member's calendar, without displacing the existing indication of how that day went for eating.
- **FR-022**: System MUST report, over a recent window, the number of days on which the member exercised and the total time spent.
- **FR-023**: System MUST exclude days before the plan start date and days in the future from all counts and totals.
- **FR-024**: System MUST return an empty state, not an error, for a member who has a plan but no recorded exercise.

**Catalogue**

- **FR-025**: System MUST provide a curated catalogue of everyday activities, each carrying the energy rate FR-017 needs, present on a first run and never duplicated by a restart.
- **FR-026**: Members MUST be able to find an activity by searching its name.
- **FR-027**: When no catalogue activity matches, System MUST tell the member it is unavailable rather than recording an entry with no activity. Members cannot define their own activities in this release.

**Access and separation**

- **FR-028**: System MUST require the member to be signed in for every exercise capability, reusing the application's existing sign-in.
- **FR-029**: System MUST derive the acting member's identity from their authenticated session and MUST NOT accept a member identity supplied in a request.
- **FR-030**: System MUST associate every exercise entry with exactly one member and MUST NOT allow a member to read or modify another member's entries.

### Key Entities *(include if feature involves data)*

- **Exercise Entry**: One activity performed on one date — which activity, how long in whole minutes, and the estimated energy used, captured when it was recorded. Many per date. Belongs to one member.
- **Activity**: A curated entry in the catalogue — a name, a category, and an energy rate. Intensity lives here rather than on the entry: "Running, easy" and "Running, fast" are separate activities. Maintained with the application, not by members.
- **Daily Activity Summary**: A date's recorded entries reduced to a total duration and a count, used by the calendar and the summary view. Derived from the entries.
- **Activity Period Summary**: Active days, total time and a recent trend over a chosen window. Derived, not stored.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A member can record an activity, from opening the day to seeing it saved, in under 20 seconds.
- **SC-002**: After any entry is added, edited or removed, the day's activity total reflects the change immediately, with no manual refresh.
- **SC-003**: Activity search returns matches in under 1 second, and 85% of searches return a usable match in the first five results when measured against a fixed corpus of everyday activities checked in with the feature.
- **SC-004**: Opening the calendar or activity summary returns results in under 1 second for a member with 3 years of daily history.
- **SC-005**: 100% of dates shown carry an exercise marking that matches the entries recorded for that date, verified across days with one entry, several entries, none, pre-plan and future dates.
- **SC-006**: Active-day counts and total durations are correct for every boundary case exercised, including a week with no activity, a day with several sessions, a history spanning a leap day, and days before the plan start.
- **SC-007**: 100% of recorded entries retain their original activity name and captured values after the catalogue is corrected.
- **SC-008**: 100% of days keep their eating assessment and calorie target unchanged when exercise is added, edited or removed — verified on days that are on target, over target and not logged, and when the exercise is recorded days later.
- **SC-009**: No screen presents the energy estimate as calories available to eat, and no figure combines the estimate with the daily target.
- **SC-010**: Restarting the application leaves exactly one copy of every curated activity.
- **SC-011**: No exercise request succeeds without a valid signed-in session, and no member can retrieve another member's entries or summaries.

### Post-Launch Outcome Measures

These describe adoption once the feature is live. Each needs usage instrumentation that this
release does not build, so they are **not release gates** and no implementation task is expected to
verify them.

- **SC-012**: 40% of members who log food also record exercise within their first fortnight.
- **SC-013**: Members who record exercise log food on 20% more days than those who do not.

## Assumptions

- Exercise logging belongs to the existing diet programme rather than becoming a separate programme of its own. The member asked for it "for diet", and the value is in seeing movement beside eating. Should exercise later grow its own goals, plans and coaching, promoting it to a peer programme is a separate decision.
- Members using this are the same members who already have accounts, and the existing sign-in is reused unchanged.
- A member must have a diet plan before recording exercise, because the plan supplies the start date that bounds valid dates. Exercise is not offered to a member who has not set one up.
- The date rules match food logging exactly — not in the future, not before the plan start — so a member does not have to learn two different sets of rules for the same calendar.
- Duration is recorded in whole minutes. Fractions of a minute carry no useful meaning here.
- The activity catalogue is curated and maintained with the application. Adding activities is a maintenance activity between releases, not something members can do — the same first-release limitation accepted for the food library, for the same reason.
- All stored instants use a single consistent time reference, and a "day" is a calendar date rather than a rolling 24-hour window. A single time zone per member is assumed for this release.
- Heart-rate and wearable import, GPS routes, sets and repetitions, structured training plans, and coaching or form guidance are all out of scope for this release.
- Energy figures are estimates for general wellbeing information, not clinical measurements, and the feature makes no claim to measure a member's actual expenditure. They are shown because members want to see them, and deliberately not converted into permission to eat more: published estimates of this kind commonly overshoot by a wide margin, and turning one into extra calories would hand a member a licence built on a guess.
- Energy rates come from a standard published table of activity energy costs, scaled by duration and the member's current weight. The specific table is a construction decision deferred to the plan.
- Keeping recorded exercise out of the target also protects an existing guarantee: a logged day keeps the target that was in force when it was logged. Were exercise to add headroom, logging a past run would retroactively change how that day was judged.
- Letting logged exercise refine the plan's declared activity level over time is a plausible later feature, deliberately left out of this release. It needs a rule for how much logged history is enough, and this release is about recording activity, not inferring from it.
- Members reach the feature through the application's existing web interface on a modern browser, at both desktop and mobile screen sizes.
