# Feature Specification: Beer Days and Calendar Activity Markings

**Feature Branch**: `005-beer-and-exercise-markings`

**Created**: 2026-09-06

**Status**: Draft

**Input**: User description: "I want to record beer dates for my diet. Give it a dedicated color marking on calendar. Excercise dates also should have a decicated color marking. The analytic charts should display these information."

## Overview

Members of the diet programme already log what they eat and record the days they exercised. There
is one habit that shapes a diet week and is currently invisible: drinking beer. A member who is
trying to eat well knows the beer nights are where the plan slips, but the programme has no way to
mark them, so they cannot see the pattern.

This feature lets a member mark a day as a beer day, gives beer days their own marking on the diet
calendar, promotes exercise days to an equally clear dedicated marking (today they show only as a
faint dot), and surfaces both facts in the diet analytics so a member can see how often each
happens and how beer days line up with the days their eating went over target.

Beer days are the only new thing a member logs. Exercise days and eating history already exist and
are read as-is.

## Scope

In scope:

- Marking and unmarking a beer day (US1).
- A dedicated, distinct marking for beer days and for exercise days on the diet calendar, in both
  the month and year views, without either marking hiding the day's eating state (US2).
- Beer frequency, exercise frequency, and a beer-day vs non-beer-day eating-outcome comparison in
  the existing diet analytics view (US3).

Out of scope for this release:

- A detailed drink log (brand, style, time of day, venue). A beer day is a day-level marker.
- Tracking other alcohol (wine, spirits) as their own categories. If the member drank, it is a
  beer day.
- Any health or clinical guidance about alcohol. Consistent with the analytics feature, the
  programme reflects what happened and does not advise.
- Changing how exercise is logged. This feature only changes how an exercise day is *shown*.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Mark a day as a beer day (Priority: P1)

A member had a few beers last night. They open the diet calendar, pick yesterday, and mark it as a
beer day. The day now carries a beer marking. Later they realise they picked the wrong day and
remove the marking.

**Why this priority**: This is the core request. Without the ability to record a beer day, nothing
else in the feature has data to show.

**Independent Test**: Mark several past dates as beer days, reload the calendar, and confirm each
marked day shows the beer marking and unmarked days do not; remove one and confirm it clears.

**Acceptance Scenarios**:

1. **Given** a member viewing the diet calendar, **When** they mark a past date within their plan
   as a beer day, **Then** that day shows the beer marking and the marking persists across reloads.
2. **Given** a day already marked as a beer day, **When** the member removes the marking, **Then**
   the day no longer shows it.
3. **Given** a beer day that is also over target and also has exercise, **When** the member looks
   at the calendar, **Then** the over-target eating state, the beer marking, and the exercise
   marking are all visible on that day at once.
4. **Given** a date before the member's plan started or a future date, **When** they try to mark it
   as a beer day, **Then** the calendar does not allow it.

---

### User Story 2 - Tell beer days and exercise days apart at a glance (Priority: P2)

A member scans the month. Without opening any day, they can see which days they exercised and which
days they drank beer, and these are obviously different from each other and from the on-target /
over-target / not-logged colours the calendar already uses.

**Why this priority**: The value of marking beer days is the pattern it reveals over a month. A
marking that is hard to distinguish from the eating-state colours, or from the exercise marking,
defeats the purpose. Exercise is included here because the current faint dot is not a "dedicated
marking" and would lose the comparison the member is trying to make.

**Independent Test**: Build a month containing every combination (plain logged day, over-target
day, beer-only day, exercise-only day, beer+exercise day, beer+exercise+over-target day) and
confirm a viewer can name each day's facts correctly from the calendar alone, including in the
year view.

**Acceptance Scenarios**:

1. **Given** a month with beer days, exercise days, and days that are both, **When** the member
   views the calendar, **Then** beer days, exercise days, and days that are both are each visually
   distinct.
2. **Given** the calendar legend, **When** the member reads it, **Then** it names the beer marking
   and the exercise marking.
3. **Given** the year view, **When** the member looks at a month, **Then** beer days and exercise
   days are still distinguishable at the smaller size.
4. **Given** a member who cannot rely on colour alone, **When** they view the calendar, **Then**
   beer days and exercise days are still distinguishable (shape, position, or symbol, not only
   hue).

---

### User Story 3 - See beer and exercise in analytics (Priority: P3)

After a month, the member opens diet analytics. Alongside the existing breakdowns they see how many
beer days there were and how that compares to the weeks before, how many days they exercised, and a
plain comparison of how their eating went on beer days versus every other day.

**Why this priority**: This turns the marked days into the insight the member actually wants — "the
beer nights are the problem" — stated in numbers. It depends on US1 having collected the data.

**Independent Test**: Log a period with a known number of beer days and exercise days and a known
spread of eating-state verdicts, open analytics for that period, and confirm the beer count,
exercise count, and beer-day vs non-beer-day comparison match the data.

**Acceptance Scenarios**:

1. **Given** a selected analytics period, **When** the member opens analytics, **Then** they see
   the number of beer days and the number of exercise days in that period.
2. **Given** a selected analytics period with beer days and non-beer days that have eating-state
   verdicts, **When** the member opens analytics, **Then** they see how eating outcomes on beer
   days compare with non-beer days.
3. **Given** the member changes the analytics period, **When** the view updates, **Then** the beer
   and exercise figures update to the same period as the rest of analytics.
4. **Given** a period with no beer days, **When** the member opens analytics, **Then** the beer
   section reports zero rather than being blank or broken.

---

### Edge Cases

- A day that is simultaneously over target, a beer day, and an exercise day — all three facts must
  remain visible (covered by FR-007).
- A beer day on which nothing was eaten (no food logged) — the day is "not logged" for eating and
  still a beer day.
- Marking a beer day, then later the member's plan start date moves — pre-plan beer days are
  retained as data but shown consistently with how the calendar treats other pre-plan days.
- The member marks a beer day for today, then exercises later the same day — both markings apply to
  the same day.
- A very large intake ("I drank a lot") — the marking still represents a single beer day; quantity
  handling depends on FR-004.
- Analytics period contains days outside the member's plan — those days are excluded from the
  comparison consistently with the rest of analytics.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Members MUST be able to mark a specific calendar date as a beer day from the diet
  calendar.
- **FR-002**: Members MUST be able to remove a beer-day marking from a date.
- **FR-003**: Members MUST be able to mark beer days for past dates on or after their plan start
  date and up to and including today; future dates MUST NOT be markable.
- **FR-004**: A beer-day record MUST capture only the fact that beer was consumed on that date. It
  MUST NOT capture an amount and MUST NOT contribute calories to the day's intake or affect the
  day's eating-state verdict.
- **FR-005**: The diet calendar MUST show every beer day with a dedicated marking that is visually
  distinct from the eating-state markings (on target, over target, not logged, outside plan) and
  from the exercise marking.
- **FR-006**: The diet calendar MUST show every exercise day with a dedicated marking that is
  visually distinct from the eating-state markings and from the beer marking, replacing the current
  faint indicator with a first-class marking.
- **FR-007**: When a day is at once a beer day, an exercise day, and has an eating state, the
  calendar MUST keep all three facts visible; no marking may replace or obscure another. The
  eating-state colour stays as the day's fill; the beer marking and the exercise marking are each
  shown as their own distinct indicator on the cell (for example a corner mark, stripe, or border)
  rather than by changing the day's fill colour.
- **FR-008**: The calendar legend MUST explain both the beer marking and the exercise marking.
- **FR-009**: The beer and exercise markings MUST appear in both the month view and the year view
  of the calendar.
- **FR-010**: Beer days MUST NOT change the diet logging streak, the "days logged" count, or the
  average-calorie figures shown elsewhere in the programme.
- **FR-011**: Diet analytics MUST show the number of beer days in the selected period and an
  indication of how that rate compares over time (e.g. beer days per week across the period).
- **FR-012**: Diet analytics MUST show the number of exercise days in the selected period.
- **FR-013**: Diet analytics MUST show how eating-state outcomes on beer days compare with
  non-beer days over the selected period.
- **FR-014**: The beer and exercise information in analytics MUST use the same period the member
  has selected for the rest of the analytics view.
- **FR-015**: Analytics MUST handle a period with zero beer days or zero exercise days by reporting
  zero, not by failing or hiding the section.
- **FR-016**: A member MUST only be able to see and manage their own beer days.
- **FR-017**: At most one beer-day marking MUST exist per date per member; marking an
  already-marked day has no additional effect.

### Key Entities *(include if feature involves data)*

- **Beer Day**: A member's record that beer was consumed on a specific calendar date. Belongs to
  one member; at most one per date. May carry an amount depending on FR-004.
- **Exercise Day** (existing): A date on which the member recorded activity. Read by this feature
  for its calendar marking and analytics count; not modified.
- **Diet Day / Eating State** (existing): The logged state and eating-state verdict for a date
  (on target, over target, not logged, outside plan). Read by this feature for the beer-day vs
  non-beer-day comparison; not modified.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A member can mark or unmark a beer day in under 10 seconds from the calendar.
- **SC-002**: In a month containing beer days, exercise days, and every eating state, 100% of
  marked days are correctly identifiable as beer, exercise, or both by looking at the calendar
  alone.
- **SC-003**: On a day that is over target, a beer day, and an exercise day, a member can identify
  all three facts without opening the day.
- **SC-004**: For any selected analytics period, the member can read the beer-day count, the
  exercise-day count, and a beer-day vs non-beer-day eating comparison, and each figure matches the
  underlying records exactly.
- **SC-005**: Beer and exercise markings remain distinguishable from each other and from
  eating-state colours for a viewer with common colour-vision deficiency (they differ by more than
  hue).
- **SC-006**: Adding beer-day data does not change any existing streak, days-logged, or
  average-calorie value for the same history.

## Assumptions

- Beer days follow the same date rules as exercise days: any date from plan start up to today,
  freely markable and removable, never in the future.
- A beer day is a lightweight day-level marker. Brand, style, time of day, and venue are out of
  scope.
- Any alcohol counts as a beer day for this release; there is no separate wine or spirits category.
- The "dedicated marking" for exercise replaces the current faint dot with a clearer, distinct
  on-cell indicator (not a change to the day's fill colour); the exercise data itself (from the
  exercise-logging feature) is unchanged.
- Beer days are recorded from the diet calendar only; there is no separate beer-logging screen.
- The analytics additions live inside the existing diet analytics view and reuse its existing
  period selector rather than introducing a separate screen.
- The beer-day vs non-beer-day comparison uses the eating-state verdicts the programme already
  computes; it does not introduce a new judgement of a day.
- The programme gives no advice about alcohol; it records and reflects, consistent with the
  analytics feature's stated scope.
- Beer days do not participate in streaks or "clean day" counts in this release.
