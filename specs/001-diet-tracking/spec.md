# Feature Specification: Diet Tracking

**Feature Branch**: `001-diet-tracking`

**Created**: 2026-09-02

**Status**: Draft

**Input**: User description: "Build another subdomain for the current healthcare application. It is about diet."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Set Up a Personal Diet Plan (Priority: P1)

A signed-in member opens the diet area for the first time. They state what they are trying to achieve (lose weight, maintain their current weight, gain weight, or simply eat more consistently), the date the plan starts, their target weight if they have one, and the body details needed to size a daily target — height, current weight, age, sex, and how active they are. The system proposes a daily calorie target and macronutrient split based on those details, explains that it is a suggestion rather than clinical advice, and lets the member accept it or replace it with a number of their own. The saved plan then shows the targets they will be measured against each day.

**Why this priority**: Nothing else in the subdomain can be measured without a target, and a member who does not know what a sensible target looks like is stuck at the first step. A member who completes only this story already receives value — a personalised daily number, calculated for them and adjustable — and it is the smallest slice that can be built, demonstrated, and shipped on its own.

**Independent Test**: Sign in, enter goal, start date, and body details; confirm a suggested target appears and is explained as a suggestion; accept it, reload the page, and confirm the saved plan shows the correct goal type, start date, and daily targets. Repeat overriding the suggestion with a custom number and confirm the override is what gets saved. Delivers a persisted, editable daily goal with no logging capability present.

**Acceptance Scenarios**:

1. **Given** a signed-in member with no diet plan, **When** they submit a goal type, start date, and their body details, **Then** a suggested daily calorie target and macronutrient split are calculated and shown, labelled as a suggestion.
2. **Given** a member viewing a suggested target, **When** they accept it, **Then** the plan is saved with the suggested value and the plan records that the target was accepted from the suggestion.
3. **Given** a member viewing a suggested target, **When** they replace it with their own value and save, **Then** the plan is saved with their value and the plan records that the target was set by the member.
4. **Given** a member whose body details would produce a target below the safe minimum, **When** the suggestion is calculated, **Then** the system suggests the safe minimum rather than the lower figure.
5. **Given** a member overriding their target with a value below the safe minimum, **When** they save, **Then** they are warned that the value is below the recommended floor and may still proceed.
6. **Given** a member who already has a plan, **When** they open the diet area, **Then** they see their existing plan rather than the setup flow.
7. **Given** a member with an existing plan, **When** they change their daily calorie target, **Then** days assessed from that point forward use the new target and already-assessed days retain the target that was in force when they were logged.
8. **Given** a member who updates their current weight or activity level, **When** the change is saved, **Then** a refreshed suggestion is offered and their existing target is left unchanged until they confirm the new one.
9. **Given** a member entering a start date later than today, **When** they attempt to save, **Then** the plan is rejected with a message explaining the start date cannot be in the future.
10. **Given** a member entering a daily calorie target of zero or less, **When** they attempt to save, **Then** the plan is rejected with an explanatory message.
11. **Given** a visitor who is not signed in, **When** they attempt to view or create a plan, **Then** access is refused.

---

### User Story 2 - Log What I Ate and See Where the Day Stands (Priority: P2)

Throughout the day a member records what they ate. For each item they search the food library by name, pick the matching food, choose a serving size and how many servings, and assign it to breakfast, lunch, dinner, or a snack. The system reads the nutrition values from the library so the member never has to know the numbers. After each entry it shows the running total for that day against their target: how much they have consumed, how much headroom is left, and whether the day is currently under, on, or over target. They can correct or remove an entry they got wrong, and they can fill in a day they forgot to log.

**Why this priority**: This is the daily engagement loop and the reason a member returns. It is meaningful only once a target exists (User Story 1), but it can be built, tested, and demonstrated as a standalone slice on top of it.

**Independent Test**: With a plan in place, search the library, add several entries across different meals for today, and confirm the day total and remaining headroom update correctly after each one. Then edit one entry's quantity and delete another and confirm the totals adjust. Delivers a complete daily food diary without any history, weight, streak, or achievement views.

**Acceptance Scenarios**:

1. **Given** a member with an active plan and no entries for today, **When** they view today, **Then** consumed shows zero and remaining equals their daily target.
2. **Given** a member typing part of a food's name, **When** the search runs, **Then** matching library items are offered with their serving sizes.
3. **Given** a member selecting a library item, a serving size, a quantity, and a meal type, **When** the entry is saved, **Then** its nutritional contribution is taken from the library item for that serving multiplied by the quantity, the day's consumed total increases by that amount, and the remaining headroom decreases by the same amount.
4. **Given** a member searching for a food the library does not contain, **When** no match is found, **Then** they are told the food is not available and no entry is created.
5. **Given** a member whose consumed total for the day exceeds their target, **When** they view the day, **Then** the day is shown as over target together with the size of the overage.
6. **Given** a member with a logged entry, **When** they edit its serving size or quantity, **Then** the day's totals recalculate immediately.
7. **Given** a member with a logged entry, **When** they delete it, **Then** the entry disappears and the day's totals recalculate.
8. **Given** a food whose library nutrition values are corrected after a member logged it, **When** the member views the day they logged it on, **Then** the entry still shows the values that were in force when it was logged.
9. **Given** a member who forgot to log two days ago, **When** they select that past date and add entries, **Then** the entries are recorded against that date and that day's state updates accordingly.
10. **Given** a member selecting a date in the future, **When** they attempt to log an entry, **Then** the entry is rejected with a message explaining future dates cannot be logged.
11. **Given** a member selecting a date before their plan's start date, **When** they attempt to log an entry, **Then** the entry is rejected with a message explaining the date precedes the plan.
12. **Given** a member attempting to view or modify a food entry belonging to another member, **When** the request is made, **Then** access is refused.
13. **Given** the same day open on two devices, **When** the second device saves a change based on a copy of the day the first device has already changed, **Then** the second save is refused with a message asking the member to reload, and no entry from either device is lost.

---

### User Story 3 - Review My History and Consistency (Priority: P3)

A member wants to know how they have actually been doing, not just how today is going. They open a calendar covering a month or a year and see each day marked as on target, over target, or not logged. Alongside it they see their current run of consecutive on-target days, their longest run to date, how many days they have logged in total, and how their average daily intake has trended over recent weeks.

**Why this priority**: Consistency is what changes eating behaviour, and a visible run of good days is the strongest retention mechanic. It is valuable but depends on accumulated logging data, so it follows User Story 2.

**Independent Test**: Seed a range of logged days with mixed outcomes, open the calendar for the covering month and year, and confirm each day is marked correctly and that the current streak, longest streak, days logged, and average intake match the seeded data. Delivers full historical insight without weight tracking, achievements, or guidance content.

**Acceptance Scenarios**:

1. **Given** a member with a mix of on-target, over-target, and unlogged days, **When** they open the calendar, **Then** each day carries the marking that matches its outcome.
2. **Given** a member who has been on target for the last five consecutive days, **When** they view their statistics, **Then** the current streak reads five days.
3. **Given** a member whose streak is broken by an over-target day, **When** they view their statistics, **Then** the current streak restarts from the day after the break and the longest streak retains the previous best.
4. **Given** a member with no logged days at all, **When** they view their statistics, **Then** all counts read zero and an empty state is shown rather than an error.
5. **Given** a member switching the calendar from a month view to a year view, **When** the view changes, **Then** the same days carry consistent markings in both views.
6. **Given** a member viewing a period that begins before their plan started, **When** the view renders, **Then** days before the plan start are shown as outside the plan rather than as missed days.

---

### User Story 4 - Watch My Weight Move Toward the Goal (Priority: P4)

A member steps on the scales every so often and records what it says. The system keeps one reading per date and shows the readings as a trend over a period they choose, together with how much their weight has changed since the plan started and how far it still is from their target weight. The most recent reading is also what the system uses when it refreshes their suggested daily target.

**Why this priority**: For a member whose goal is to lose or gain weight, this is the payoff that daily logging is working toward, and it closes the loop with the target suggestion from User Story 1. It sits below the daily loop because the feature is still useful without it, and it can be added as a self-contained slice.

**Independent Test**: Record weight readings across several dates, confirm the trend renders in date order with change-since-start and distance-to-target both correct, record a second reading for a date that already has one and confirm it replaces rather than duplicates, and confirm the newest reading is the one used when a target suggestion is refreshed. Delivers weight tracking without touching food logging.

**Acceptance Scenarios**:

1. **Given** a member with a plan, **When** they record a weight reading for today, **Then** it is saved against today's date and appears in their trend.
2. **Given** a member who already recorded a reading for a date, **When** they record another for that same date, **Then** the existing reading is replaced rather than a second one being added.
3. **Given** a member with several readings, **When** they view their trend for a chosen period, **Then** readings appear in date order with the change since plan start and the remaining difference to their target weight both shown.
4. **Given** a member with a target weight who reaches it, **When** they view their trend, **Then** the goal is shown as reached.
5. **Given** a member recording a reading dated in the future, **When** they attempt to save, **Then** it is rejected with an explanatory message.
6. **Given** a member recording a weight outside the plausible human range, **When** they attempt to save, **Then** it is rejected with an explanatory message.
7. **Given** a member whose plan holds exactly one weight reading, **When** they attempt to delete it, **Then** the deletion is refused with a message explaining they can correct the reading instead.
8. **Given** a member with no readings within the period they are viewing, **When** they view their trend, **Then** an empty state for that period is shown rather than an error.
9. **Given** a member who records a new current weight, **When** a target suggestion is next refreshed, **Then** the refreshed suggestion uses that newest reading.

---

### User Story 5 - Earn Recognition for Sticking With It (Priority: P5)

As a member accumulates on-target days, logging consistency, and time on plan, the system awards named achievements — first day logged, a full week on target, a full month on target, thirty days logged, and similar. Unlocked achievements are shown with the date they were earned; locked ones are shown with what is still required to reach them.

**Why this priority**: Recognition reinforces the habit but adds nothing until there is history to recognise. It is a self-contained slice that can be added without touching the logging flow.

**Independent Test**: Seed a member's history to just below an achievement threshold, log the qualifying day, and confirm the achievement unlocks with the correct earned date while unrelated achievements remain locked with accurate remaining-progress text.

**Acceptance Scenarios**:

1. **Given** a member who has just met an achievement's criteria, **When** their achievements are evaluated, **Then** that achievement is unlocked and stamped with the date it was earned.
2. **Given** a member who has already unlocked an achievement, **When** their achievements are evaluated again, **Then** it remains unlocked once, keeps its original earned date, and is not duplicated.
3. **Given** a member who has not met an achievement's criteria, **When** they view their achievements, **Then** it is shown as locked with the remaining requirement stated.
4. **Given** a member who deletes entries such that a previously qualifying day no longer qualifies, **When** they view their achievements, **Then** achievements already earned remain earned.

---

### User Story 6 - Get Guidance When I Need It (Priority: P6)

A member who is struggling — over target, or facing a craving — opens a guidance area and receives practical eating tips together with a short encouraging message reflecting how their plan is going. The content comes from a curated library maintained with the application; it is general wellbeing guidance, not personalised clinical advice.

**Why this priority**: It improves the experience and mirrors the support content the smoking-cessation area already offers, but nothing else depends on it, so it ships last.

**Independent Test**: Open the guidance area as a member with an active plan and confirm tips are returned from the curated library and that the encouragement message reflects the member's current progress. Delivers supportive content with no dependency on achievements.

**Acceptance Scenarios**:

1. **Given** a signed-in member with an active plan, **When** they open the guidance area, **Then** they receive one or more eating tips from the curated library.
2. **Given** a member currently on a run of on-target days, **When** they request encouragement, **Then** the message reflects their current progress.
3. **Given** a member with no logged days, **When** they request encouragement, **Then** a getting-started message is returned rather than an error.
4. **Given** the application has been restarted, **When** the curated library is inspected, **Then** it contains exactly one copy of each item.

---

### Edge Cases

- **Day boundaries**: A member logs an entry near midnight, or travels between time zones. The calendar day an entry belongs to must be unambiguous and must not shift retroactively.
- **Target changed mid-history**: A member lowers their calorie target after weeks of logging. Days already assessed must not silently flip from on target to over target.
- **Suggestion drifting from the member's choice**: A member who overrode the suggestion months ago changes their weight. The refreshed suggestion must be offered, never silently applied over their chosen target.
- **A day with no entries**: An unlogged day must be distinguishable from a day logged with zero intake; only one of them should break a streak, and which one must be explicit.
- **Deleting the last entry of a day**: The day must revert to unlogged rather than becoming a zero-intake day — or the opposite — but the behaviour must be stated and consistent with the streak rule above.
- **Food missing from the library**: A member who cannot find what they ate has no way to log it in this release. They must be told plainly rather than left guessing, and the gap must not be papered over with a silently zero-calorie entry.
- **Library values corrected after the fact**: Correcting a food's nutrition values must not retroactively change days a member already logged and already saw assessed.
- **Serving sizes**: A food offered in more than one serving size must compute the same contribution for equivalent amounts expressed differently, and a fractional quantity must be supported.
- **Dates outside the plan**: Days before the plan start and days in the future are neither successes nor misses and must not be counted in totals, streaks, or averages.
- **Implausible values**: A single entry claiming an extreme calorie count, a negative quantity, or a quantity of zero must be rejected rather than silently distorting statistics. The same applies to weight readings outside the plausible human range.
- **Two weight readings on one date**: The later reading replaces the earlier one; a date never carries two.
- **Deleting the only weight reading**: Refused under FR-046, because the plan's target suggestion depends on a current weight. Correcting a mistyped reading is an edit, not a delete followed by a re-entry.
- **Unsafe self-set target**: A member is allowed to set a target below the recommended floor, but must be warned, and the warning must not be suppressible into silence.
- **Very long histories**: A member with several years of daily entries must still be able to open a calendar, trend, or statistics view without noticeable delay.
- **Abandoning and restarting**: A member who stops logging for months and then returns must resume against their existing plan without losing historical achievements or weight history.
- **Repeated foods**: Logging the same food three times in one day must count three times, not be collapsed into one entry.
- **Leap days and month lengths**: February 29 and 30-day months must render and count correctly in both the month and year views.
- **Concurrent edits**: The same day edited from two devices at once must not lose entries or produce a total inconsistent with the entries shown. Resolved by FR-045 — the losing write is refused and the member reloads, rather than the two writes being merged.

## Requirements *(mandatory)*

### Functional Requirements

**Plan management**

- **FR-001**: System MUST allow a signed-in member to create exactly one active diet plan, recording a goal type, a start date, their body details, their activity level, an optional target weight, and daily nutrition targets.
- **FR-002**: System MUST reject a plan whose start date is later than the current date.
- **FR-003**: System MUST reject a plan whose daily calorie target is zero or negative.
- **FR-004**: System MUST allow a member to update their plan's goal type, start date, body details, activity level, target weight, and daily targets at any time, and MUST preserve the target that applied to each already-assessed day.
- **FR-005**: System MUST calculate a suggested daily calorie target and macronutrient split from the member's height, current weight, age, sex, activity level, and goal type, and MUST present it as a suggestion the member may accept or replace with a value of their own.
- **FR-006**: System MUST record whether the target in force was accepted from the suggestion or set by the member, and MUST show the member which of the two applies.
- **FR-007**: System MUST NOT suggest a daily calorie target below the documented safe minimum for the member's sex; where the calculation produces a lower figure, the safe minimum MUST be suggested instead.
- **FR-008**: System MUST allow a member to override their target below the safe minimum, and MUST warn them that the value is below the recommended floor before the override is saved.
- **FR-009**: When a member's body details, activity level, or goal type change, System MUST offer a refreshed suggestion and MUST leave the target in force unchanged until the member confirms the new value.
- **FR-010**: System MUST present suggested targets as general wellbeing guidance rather than clinical advice, stated wherever a suggestion is shown.
- **FR-011**: System MUST associate every plan with exactly one member and MUST NOT allow a member to read or modify another member's plan.

**Body measurements**

- **FR-012**: System MUST allow a member to record a body-weight reading against a calendar date, holding at most one reading per date, with a later reading for the same date replacing the earlier one.
- **FR-013**: Members MUST be able to edit and delete their own weight readings.
- **FR-014**: System MUST reject weight readings dated in the future and readings outside the plausible human range.
- **FR-015**: System MUST present weight readings as a trend over a member-chosen period, in date order, together with the change since the plan's start date and the remaining difference to the target weight where one is set.
- **FR-016**: System MUST indicate when a member's target weight has been reached.
- **FR-017**: System MUST use the member's most recent weight reading as their current weight when calculating a target suggestion.
- **FR-018**: System MUST return an empty state, not an error, for a member with a plan and no weight readings.

**Food library**

- **FR-019**: System MUST provide a curated food library in which each item carries a name, one or more named serving sizes, and nutrition values for each serving size.
- **FR-020**: Members MUST be able to find a library item by searching on its name, with matches offered as they type.
- **FR-021**: System MUST seed the food library on first run and MUST NOT duplicate its contents when the application restarts.
- **FR-022**: When a member's search returns no match, System MUST tell them the food is not available and MUST NOT create an entry. Members cannot define their own foods in this release.

**Daily logging**

- **FR-023**: System MUST allow a member to record food entries against a calendar date, each referencing a food library item, a serving size, a quantity, and a meal type of breakfast, lunch, dinner, or snack.
- **FR-024**: System MUST derive an entry's nutritional contribution from the library item's values for the chosen serving size multiplied by the quantity, and MUST support fractional quantities.
- **FR-025**: System MUST preserve the nutrition values that were in force when an entry was logged, so that later corrections to the food library do not retroactively alter assessed history. When a member edits an entry themselves, System MUST re-read the values from the library for the serving then in force and re-snapshot them, because a member's own edit is a deliberate act rather than a background correction.
- **FR-026**: System MUST reject food entries dated in the future and entries dated before the plan's start date.
- **FR-027**: System MUST reject food entries whose quantity is zero or negative, and entries whose calorie contribution exceeds a plausible single-entry ceiling.
- **FR-028**: Members MUST be able to edit and delete their own food entries, and totals MUST reflect the change immediately.
- **FR-029**: System MUST record each entry against an unambiguous calendar day that does not change once recorded.
- **FR-030**: System MUST allow entries to be added to any past date on or after the plan start date, so a member can fill in days they missed.

**Daily assessment**

- **FR-031**: System MUST compute, for any given day, the total consumed, the amount remaining against that day's target, and whether the day is under, on, or over target.
- **FR-032**: System MUST distinguish three day states — on target, over target, and not logged — and MUST treat a day with no entries as not logged rather than as a fully compliant day.
- **FR-033**: System MUST derive every daily total from that day's entries alone, so that adding, editing, or removing an entry always yields a total consistent with the entries shown.

**History and statistics**

- **FR-034**: System MUST present a calendar of the member's history for a chosen month and for a chosen year, marking each day with its state.
- **FR-035**: System MUST report the member's current consecutive run of on-target days, their longest such run to date, the total number of days logged, and their average daily intake over a recent window.
- **FR-036**: System MUST exclude days before the plan start date and days in the future from all counts, streaks, and averages.
- **FR-037**: System MUST return an empty state, not an error, for a member who has a plan but no entries.

**Recognition and guidance**

- **FR-038**: System MUST award achievements when a member meets defined thresholds for consecutive on-target days, total days logged, and time elapsed on plan, recording the date each was earned.
- **FR-039**: System MUST award each achievement at most once per member and MUST NOT revoke an achievement already earned.
- **FR-040**: System MUST show locked achievements together with what remains to be done to unlock them.
- **FR-041**: System MUST provide eating tips and a progress-aware encouragement message drawn from a curated library that is present on a first run and is never duplicated by a restart.

**Access and separation**

- **FR-042**: System MUST require the member to be signed in for every diet capability, reusing the application's existing sign-in so that one sign-in covers this area and the areas that already exist.
- **FR-043**: System MUST derive the acting member's identity from their authenticated session and MUST NOT accept a member identity supplied in a request.
- **FR-044**: System MUST keep diet information separate from the smoking-cessation area, such that neither area reads the other's stored data and neither becomes unavailable when the other does.

**Data consistency**

- **FR-045**: System MUST detect when a logged day has been changed by another session since it was read, and MUST refuse the conflicting write with a message telling the member to reload — rather than silently discarding entries or leaving a day's recorded total inconsistent with the entries it holds.
- **FR-046**: System MUST NOT allow a member to delete their only remaining weight reading, because the plan's target suggestion depends on a current weight. A member correcting a mistaken reading edits it instead.

### Key Entities *(include if feature involves data)*

- **Diet Plan**: A member's standing intent — goal type, start date, body details, activity level, optional target weight, and the daily nutrition targets they are measured against, along with whether those targets were suggested or member-set. One active plan per member; owns the days logged beneath it.
- **Logged Day**: A single calendar date under a plan, holding that date's food entries and deriving its own totals and on-target/over-target state from them.
- **Food Entry**: One thing eaten on a logged day — the library item referenced, the serving size and quantity chosen, the meal type, and the nutritional contribution captured at the time of logging.
- **Nutrition Target**: The daily amounts a member is aiming for, together with the record of which target applied on a given day so that history stays stable when targets change.
- **Food Library Item**: A curated food with a name, named serving sizes, and nutrition values per serving. Maintained with the application and seeded on first run.
- **Weight Reading**: A member's body weight on a given date, at most one per date, forming the trend and supplying the current weight used by target suggestions.
- **Diet Achievement**: A named milestone with an unlock condition, and per member the date it was earned.
- **Eating Tip**: A piece of curated guidance content shown to members who need support.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new member can go from opening the diet area to having a saved plan with daily targets in under 2 minutes, including entering their body details.
- **SC-003**: A member can record a single food entry, from starting the search to seeing the updated day total, in under 20 seconds.
- **SC-004**: Food search returns matches in under 1 second, and 85% of searches return a usable match in the first five results when measured against a fixed corpus of everyday foods defined and checked in with the feature.
- **SC-005**: After any entry is added, edited, or removed, the day's totals shown to the member reflect the change immediately, with no manual refresh.
- **SC-006**: Opening the calendar, weight trend, or statistics view returns results in under 1 second for a member with 3 years of daily history.
- **SC-007**: 100% of days shown in the calendar carry a state matching the entries recorded for that date, verified across on-target, over-target, unlogged, pre-plan, and future days.
- **SC-008**: Streak, longest-streak, days-logged, and average-intake figures are correct for every boundary case exercised, including a broken streak, a single-day streak, an all-unlogged history, and a history spanning a leap day.
- **SC-009**: 100% of assessed days retain their original assessment after a food library correction and after a target change.
- **SC-010**: No target below the safe minimum is ever suggested, and 100% of member overrides below that floor produce a warning before saving.
- **SC-011**: Restarting the application leaves exactly one copy of every curated food, tip, and achievement definition.
- **SC-012**: No diet request succeeds without a valid signed-in session, and no member can retrieve another member's plan, entries, weight readings, or statistics.

### Post-Launch Outcome Measures

These describe adoption once the feature is live. Each needs usage instrumentation that this release
does not build, so they are **not release gates** and no implementation task is expected to verify
them. They are recorded here so they are not mistaken for acceptance criteria.

- **SC-002**: 70% of members accept the suggested daily target rather than replacing it, indicating the suggestion is credible.
- **SC-013**: 60% of members who log a first day return to log a second day within 7 days.
- **SC-014**: 90% of members complete their first food entry without abandoning the flow.

## Assumptions

- Members using the diet area are the same members who already have accounts in the application, and the existing sign-in is reused unchanged — no separate registration, and one sign-in grants access to both the diet area and the areas that already exist.
- Diet is a separate area of the application with its own stored information. It does not read or write the smoking-cessation area's data; the only thing the two have in common is the identity of the signed-in member. Body details needed for target suggestions are held by the diet area itself rather than read from elsewhere.
- The diet area follows the shape members already know from the smoking-cessation area — a one-time setup, a daily action, a colour-coded calendar with streaks, achievements, and supportive content — because that familiarity lowers the cost of adopting it.
- Target suggestions are produced by a standard published resting-energy estimate adjusted for activity level and goal type. The specific formula is a construction decision and is deferred to the plan.
- The safe minimum daily calorie figure is a curated, reviewable value rather than something derived per member. Commonly cited floors of 1,200 calories per day for women and 1,500 for men are the working default, subject to review before release.
- Suggested targets and guidance content are general wellbeing information. The feature makes no claim to diagnose, treat, or prescribe, and every suggestion is overridable by the member.
- All stored instants use a single consistent time reference, and a "day" is a calendar date rather than a rolling 24-hour window. A single time zone per member is assumed for the first release; handling travel across time zones is out of scope.
- Nutrition is tracked in calories plus the three macronutrients — protein, carbohydrate, and fat. A member may leave macronutrient targets unset and still use the feature; calorie targets are mandatory.
- Weights and heights are stored in a single consistent unit system, with the display unit (kilograms or pounds, centimetres or feet and inches) a member preference that does not affect stored values.
- The food library is curated and maintained with the application. Adding foods is a maintenance activity between releases, not something members can do — a deliberate first-release limitation accepted in exchange for trustworthy nutrition data.
- A plan always carries at least one weight reading, because setup requires a current weight and the last remaining reading cannot be deleted. Current weight therefore always has a source.
- A member has one active plan at a time. Keeping several plans in parallel, and sharing a plan with a coach or clinician, are out of scope for this release.
- Photo capture of meals, barcode scanning, wearable or fitness-tracker import, recipe and meal-plan building, member-defined foods, and water-intake tracking are all out of scope for this release.
- Curated reference content — foods, tips, and achievement definitions — is seeded on first run in the same manner as the curated content that already exists.
- Members reach the feature through the application's existing web interface on a modern browser, at both desktop and mobile screen sizes.
