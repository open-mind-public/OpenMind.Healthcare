# Feature Specification: Diet Analytics

**Feature Branch**: `003-diet-analytics`

**Created**: 2026-09-03

**Status**: Draft

**Input**: User description: "I need a powerfull analytic feature for diet"

## Overview

Members of the diet programme already log what they eat, what they weigh and what exercise they
do. Today the only thing the programme tells them back is a count: days logged, a current streak,
a longest streak and an average daily calorie figure. That answers "have I been consistent" and
nothing else.

The question a member actually asks after a month of logging is different, and the programme
cannot currently answer any of it: *where do my calories actually go, is my eating shaped the way
I intended, and when does it go wrong?* This feature turns the history they have already built up
into answers, and says in words what it notices.

It reads existing history and adds no new thing for a member to log.

## Scope

In scope: what a member eats (US1), how that compares with the targets they set (US2), when they
eat it (US3), and written observations drawn from those three (US4).

Deliberately out of scope for this release:

- **Intake, weight and activity on a shared timeline.** The most motivating view and the easiest
  to misread — it invites a "net calories" figure that would break the guarantee in FR-023. Worth
  building once the figures beneath it are trusted, not before.
- **Export and sharing.** Analytics are a view inside the app. A member who wants to show someone
  can take a screenshot; a report designed to be read out of context is its own piece of work.
- **Suggested actions.** The feature says what it sees, not what to do about it. Advice on what to
  eat is dietary advice and would need the clinical sign-off the calorie floors are already
  waiting on.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See where my calories actually go (Priority: P1)

A member has been over target more often than not and does not know why. They open analytics for
the last month and see their intake broken down: how much came from each meal, and which
individual foods contributed most. The answer is usually a small number of specific things, and
seeing them named is what makes the next month different.

**Why this priority**: It is the first question anyone asks of their own food log, it needs
nothing but data already captured, and on its own it justifies the feature. Without it the rest
is decoration.

**Independent Test**: Log a month of varied meals, open analytics, and confirm the per-meal split
sums to the period's total intake and the top contributing foods match the log.

**Acceptance Scenarios**:

1. **Given** a member with a month of logged days, **When** they open analytics for that month,
   **Then** they see total and average daily intake, a breakdown by meal, and the foods that
   contributed most, ranked.
2. **Given** the breakdown is shown, **When** the member adds the meal figures together,
   **Then** they equal the period's total intake — nothing is silently excluded or double counted.
3. **Given** a member who has logged only three days of a thirty day period, **When** they view
   the average, **Then** it is stated which days it is averaged over, so a three day average is
   not mistaken for a monthly one.
4. **Given** a member with no logged days in the period, **When** they open analytics,
   **Then** they are told there is nothing to analyse yet and what to do about it.

---

### User Story 2 - See whether my eating matches my targets (Priority: P2)

A member set a calorie target and macronutrient targets when they made their plan, but only the
calorie one has ever been reported back to them. They open analytics and see how their protein,
carbohydrate and fat intake compares with what they were aiming for, across the period rather than
on a single day.

**Why this priority**: The targets already exist and are already stored against every logged day;
not reporting on them wastes something the member already committed to. It ranks below US1 because
a member who does not yet know where their calories go is not ready to tune macronutrients.

**Independent Test**: Log days whose macronutrient split is deliberately far from target, open
analytics, and confirm the reported comparison matches a hand calculation over the same days.

**Acceptance Scenarios**:

1. **Given** a member with macronutrient targets and logged days, **When** they open analytics,
   **Then** they see their actual protein, carbohydrate and fat intake beside the target for the
   same period.
2. **Given** a day whose target differed from today's target, **When** it is included in a period,
   **Then** it is compared against the target that was in force on that day, not today's.
3. **Given** a member whose plan carries no macronutrient targets, **When** they open analytics,
   **Then** their actual split is still shown, without inventing a target to compare it against.

---

### User Story 3 - See the pattern in when I eat (Priority: P3)

A member suspects their problem is not what they eat but when. They open analytics and see how
their intake is distributed across the days of the week and across the hours of the day — the
weekend they always overshoot, or the third of their calories that arrive after nine at night.

**Why this priority**: It answers a question the per-meal breakdown hints at but cannot settle,
and every logged entry already carries the moment it was recorded. It ranks below US2 because it
describes behaviour rather than nutrition, and rests on a known approximation.

**Independent Test**: Log a fortnight with deliberately heavy weekends and late evenings, open
analytics, and confirm the reported distribution matches the seeded pattern.

**Acceptance Scenarios**:

1. **Given** a member with several weeks logged, **When** they view eating patterns, **Then** they
   see intake distributed by day of the week and by time of day.
2. **Given** a member who logs meals retrospectively at one sitting, **When** time-of-day patterns
   are shown, **Then** the limitation is stated plainly, because the recorded time is when they
   logged it and not necessarily when they ate.

---

### User Story 4 - Be told what the numbers say (Priority: P4)

A member opens analytics and, above the charts, reads a short list of what the programme noticed:
that a third of their intake is logged after nine in the evening, that Saturdays run six hundred
calories above their average, that one food accounts for a fifth of the month. Each is a fact
about their own data with the figure attached, not a verdict.

**Why this priority**: It is what turns three screens of charts into something a member acts on,
and it is the part most easily got wrong — an observation that fires on four days of data, or that
tells someone their eating is unhealthy, is worse than no observation at all. It ranks last
because every observation is drawn from figures the first three stories produce.

**Independent Test**: Seed a member with a deliberate pattern and confirm the matching observation
appears with the right figure; seed a member with too little data and confirm nothing fires.

**Acceptance Scenarios**:

1. **Given** a member whose data contains a clear pattern, **When** they open analytics,
   **Then** an observation naming that pattern appears, with the figure it rests on.
2. **Given** a member with fewer logged days than an observation requires, **When** they open
   analytics, **Then** that observation does not appear, and they are told more days are needed
   rather than shown a weak claim.
3. **Given** the same data and the same period, **When** a member views analytics twice,
   **Then** they see the same observations both times.
4. **Given** any observation, **When** it is read, **Then** it describes what the data shows and
   does not diagnose a condition, judge the member, or tell them what to eat.
5. **Given** a member whose data contains no pattern that meets its threshold, **When** they open
   analytics, **Then** they are told nothing stood out, rather than shown a manufactured
   observation.

---

### Edge Cases

- What happens when a member has a plan but has never logged anything? The feature says so and
  offers the first step, rather than showing zeroes that look like a bad month.
- What happens when the requested period predates the member's plan? Days before the plan started
  are excluded from every figure, and the period actually analysed is stated.
- What happens when a member has logged one day out of thirty? Every average states its
  denominator, so a single day cannot masquerade as a monthly average, and no observation fires.
- What happens when a period contains a day the member logged and then emptied? It counts as not
  logged, exactly as it does everywhere else in the programme.
- What happens when one mistyped entry dominates the ranking? The contribution is reported as
  logged — the analytics do not quietly discard outliers — but the member can see the individual
  entry behind any figure and correct it.
- What happens when a member changed their calorie target mid-period? Each day is compared against
  the target that was in force on that day; the period is not re-judged against today's target.
- What happens when two observations describe the same thing from different angles? Only the
  stronger is shown, so the list does not repeat itself.
- What happens when a member logs an entire week in one sitting? The time-of-day distribution
  concentrates on that sitting, which FR-015 requires the screen to acknowledge, and observations
  about time of day are held back for want of spread.
- How does the system handle a member asking for a period spanning several years of daily logging?
  It answers within the same responsiveness budget as the rest of the programme.
- What happens when a member deletes food entries after viewing analytics? The next view reflects
  the deletion; nothing is cached in a way that outlives the data it describes.

## Requirements *(mandatory)*

### Functional Requirements

#### Choosing what to analyse

- **FR-001**: Members MUST be able to view analytics over a chosen period, selected from preset
  ranges covering at least the last week, the last month, the last three months, and the whole
  plan.
- **FR-002**: System MUST exclude days before the member's plan start date and days after today
  from every figure, and MUST state the period actually analysed when it differs from the one
  requested.
- **FR-003**: System MUST state, for every average it presents, whether it is averaged over logged
  days or over all days in the period, and how many days that is.
- **FR-004**: System MUST compare the chosen period against the immediately preceding period of
  the same length, so a member can see direction and not only position.

#### Where the calories go (US1)

- **FR-005**: System MUST report total and average daily energy intake for the period.
- **FR-006**: System MUST break intake down by meal, and the parts MUST sum to the reported total.
- **FR-007**: System MUST rank the individual foods that contributed most energy over the period,
  showing for each how much it contributed and how often it was logged.
- **FR-008**: System MUST report intake grouped by the food categories the library already
  defines, so a member can see the balance of their diet without reading every entry.
- **FR-009**: System MUST report how many days in the period were on target, over target and not
  logged.

#### Targets and macronutrients (US2)

- **FR-010**: System MUST report actual protein, carbohydrate and fat intake for the period beside
  the targets in force, expressed both as amounts and as a share of intake.
- **FR-011**: System MUST compare each day against the target that was in force on that day, never
  against the member's current target.
- **FR-012**: System MUST present the macronutrient split even when the member's plan carries no
  macronutrient targets, without inventing a target for comparison.

#### Patterns (US3)

- **FR-013**: System MUST report intake distributed across the days of the week.
- **FR-014**: System MUST report intake distributed across the hours of the day.
- **FR-015**: System MUST state, wherever time of day is presented, that the time shown is when the
  entry was recorded rather than when the food was eaten.

#### Observations (US4)

- **FR-016**: System MUST present written observations drawn from the member's own data for the
  chosen period.
- **FR-017**: Every observation MUST carry the figure it rests on and the period it covers, so a
  member can check it against the breakdown beneath.
- **FR-018**: Every observation MUST have a stated minimum number of logged days and a stated
  threshold, and MUST NOT appear unless both are met.
- **FR-019**: Observations MUST describe what the data shows. They MUST NOT diagnose a condition,
  characterise the member or their eating as good or bad, or instruct them what to eat.
- **FR-020**: The same data over the same period MUST produce the same observations every time.
- **FR-021**: When no observation meets its threshold, System MUST say that nothing stood out
  rather than lowering a threshold or presenting a weaker claim.
- **FR-022**: When several observations describe the same underlying pattern, System MUST present
  only the strongest.

#### Boundaries and access

- **FR-023**: System MUST NOT subtract estimated energy from exercise from intake, add it to any
  target, or present any combined figure that reads as calories available to eat.
- **FR-024**: System MUST NOT alter any logged day, its stored target, its assessment, or the
  member's plan. Analytics are derived and read-only.
- **FR-025**: System MUST analyse only the signed-in member's own data, and MUST refuse
  unauthenticated requests.
- **FR-026**: System MUST reflect a change to underlying data the next time analytics are viewed.
- **FR-027**: System MUST make clear what each figure is derived from, so a member can trace any
  number back to the days and entries behind it.

### Key Entities *(include if feature involves data)*

- **Analysis Period**: The span being analysed, its preceding comparison span, the member's plan
  start, and how many of its days carry logged data.
- **Intake Summary**: Total and average energy over a period, with the count of days it is drawn
  from and the split of day states within it.
- **Meal Breakdown**: Energy attributed to each meal of the day across the period.
- **Food Contribution**: One food, how much energy it contributed over the period, and how many
  times it was logged.
- **Category Breakdown**: Energy attributed to each food category across the period.
- **Macronutrient Comparison**: Actual protein, carbohydrate and fat against the targets that were
  in force, as amounts and as shares.
- **Distribution**: Intake attributed to each day of the week and to each hour of the day.
- **Observation**: One thing noticed in the period — what it says, the figure behind it, the
  minimum data and threshold that let it fire, and how strongly it applies.

## Success Criteria *(mandatory)*

### Release gates

- **SC-001**: A member with a month of logged history can identify their three largest sources of
  calories within thirty seconds of opening analytics.
- **SC-002**: Every breakdown reconciles exactly with the underlying log: the parts sum to the
  reported total, with no rounding drift greater than one unit of the figure shown.
- **SC-003**: Every average displayed states the number of days it is drawn from.
- **SC-004**: Analytics over three years of daily logging return in under two seconds.
- **SC-005**: A member with a plan and no logged days sees a plain explanation and a next step,
  not an empty chart or an error.
- **SC-006**: A period spanning a change of calorie target compares each day against the target
  that was in force on it, verified against a hand calculation.
- **SC-007**: No figure anywhere in the feature combines exercise energy with intake or with a
  calorie target.
- **SC-008**: No observation appears for a member with fewer logged days than that observation
  requires, verified for every observation the system can produce.
- **SC-009**: Viewing the same period twice with unchanged data produces an identical set of
  observations.
- **SC-010**: Every observation the system can produce is reviewed against FR-019 before release:
  none diagnoses, judges or instructs.
- **SC-011**: Viewing analytics leaves every logged day, target and assessment byte-for-byte
  unchanged.
- **SC-012**: Analytics for one member never include another member's data, and unauthenticated
  requests are refused.
- **SC-013**: Every figure presented can be traced by the member to the days or entries it was
  derived from.

### Post-launch measures

- **SC-014**: Members who open analytics log at least as consistently in the following month as
  those who do not — the feature does not become a substitute for logging.
- **SC-015**: Of members with at least thirty logged days, half open analytics within a month of
  release.

## Assumptions

- The feature analyses history that already exists. It adds nothing new for a member to log and
  captures no new field at logging time.
- Analytics belong to the diet programme and cover diet data only. The quit-smoking programme has
  its own analytics and is untouched.
- A member must have a plan to have analytics; a member without one is directed to set one up, as
  everywhere else in the programme.
- Preset period ranges are sufficient for this release; an arbitrary custom date range is out of
  scope unless asked for.
- Energy intake is the primary axis of every breakdown, because it is the figure the member's
  target is expressed in.
- Time-of-day analysis uses the moment an entry was recorded, which is the only time the programme
  captures. This is a known approximation, and FR-015 requires saying so rather than hiding it.
- Days a member did not log are absences, not zero-calorie days, in every figure.
- Observations are drawn from fixed, reviewable rules over the member's own figures. They are not
  predictions, and nothing about them varies between two views of the same data.
- The guarantee established for exercise logging holds here unchanged: recorded exercise is never
  presented as calories available to eat, and never moves a day's eating assessment.
- Analytics are derived on demand from the existing log rather than maintained as a separate
  record, so they cannot drift out of step with the data they describe.

## Dependencies

- The existing diet plan, food log, food library and weight readings supply every input. No new
  source of data is introduced.
- Per-day stored targets are what make FR-011 possible; without the target that was in force on
  each day, a period spanning a target change cannot be judged honestly.
- Every logged entry carries its meal, its food, its category and the moment it was recorded.
  US1 and US3 rest entirely on fields already captured.
- Platform authentication identifies the member; the feature introduces no separate access model.
