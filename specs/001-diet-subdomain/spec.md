# Feature Specification: Diet Subdomain

**Feature Branch**: `001-diet-subdomain`

**Created**: 2026-09-01

**Status**: Draft

**Input**: User description: "Build another subdomain for the current healthcare application. It is about diet." Scoping decisions taken with the requester: the subdomain covers food logging, weight-goal tracking, and meal planning, delivered as prioritised slices; it is deployed as its own service alongside the existing ones; and food data comes from a curated catalog shipped with the product rather than an external nutrition service.

## Clarifications

### Session 2026-09-01

- Q: Where should date of birth and biological sex live, given a standard energy calculation needs
  them and the product holds neither today? → A: In the diet profile. The diet capability collects
  and owns them, keeping the bounded context self-contained with no cross-service dependency at
  setup time. Accepted cost: duplication if another subdomain later needs the same attributes.
- Q: How should a logged entry keep the nutrition values it was recorded with, when the underlying
  food can later be edited or deleted? → A: Snapshot the values onto the entry at log time. History
  is then immune to later edits and to deletion of the food, at the cost of some duplication per
  entry — negligible at the expected volume.
- Q: Which unit system should the diet features use? → A: A per-person preference chosen at setup,
  mirroring how the product already varies currency per person. Canonical storage is metric;
  presentation follows the preference.
- Q: What should happen when a daily energy target would fall below a safe floor? → A: Warn clearly
  and allow an informed adult to override. Fixed floors, not derived ones, so the explanation stays
  plain.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Log what I eat against a daily target (Priority: P1)

A signed-in person tells the product a few facts about themselves and what they are trying to
achieve. The product gives them a daily energy and macronutrient target. Through the day they
record what they ate, choosing foods from a built-in catalog or entering their own, and at any
moment they can see how much of their target they have used and how much is left. Days accumulate
into a history they can look back over.

**Why this priority**: This is the smallest slice that is useful on its own. Without it there is
nothing to plan against and nothing to explain a change in weight. It also mirrors the daily-record
rhythm the person already knows from the quit-smoking side of the product, so it needs no new
mental model.

**Independent Test**: Sign in, complete diet setup, log three foods across a day, and confirm the
day's totals, remaining allowance, and target comparison are correct — with no weight entry and no
meal plan in existence.

**Acceptance Scenarios**:

1. **Given** a signed-in person with no diet profile, **When** they complete diet setup with their
   body measurements, activity level and goal, **Then** the product presents a daily energy target
   and a macronutrient split, and explains in plain language how the target was derived.
2. **Given** a person with a daily target, **When** they record a food and a portion size for a
   meal, **Then** that entry's energy and macronutrient contribution is added to the day's totals
   and the remaining allowance decreases accordingly.
3. **Given** a day with entries, **When** the person corrects the portion size of an entry, **Then**
   the day's totals recalculate immediately and no duplicate entry is created.
4. **Given** a day with entries, **When** the person removes an entry, **Then** it no longer counts
   toward any total for that day and the day remains otherwise intact.
5. **Given** a food that is not in the catalog, **When** the person enters its name and nutrition
   values once, **Then** it becomes available to them for reuse on later days without re-entry.
6. **Given** a person viewing a past day, **When** that day is before their diet profile was
   created, **Then** the product shows the day as outside their tracked period rather than as a
   day with zero intake.
7. **Given** a person who has logged food today, **When** they view their day, **Then** they can
   see the breakdown by meal occasion (for example morning, midday, evening, snacks) and not only
   a single daily figure.
8. **Given** a person completing setup, **When** the derived or entered daily target falls below the
   safe floor, **Then** the product warns them plainly, suggests professional guidance, and lets
   them proceed if they confirm.
9. **Given** a person who chose imperial units at setup, **When** they view any weight, portion, or
   measurement, **Then** it is presented in imperial throughout, and switching the preference later
   changes only presentation, never the recorded history.

---

### User Story 2 - Track my weight toward a goal (Priority: P2)

The person records their weight from time to time. The product shows the trend over the period they
have been tracking, how far they are from the goal they set, and marks the progress milestones they
have passed. Because day-to-day weight fluctuates for reasons unrelated to diet, the product
emphasises the trend over any single reading.

**Why this priority**: It is the outcome the person actually cares about, and it is independently
useful — someone can track weight without logging a single meal. It is second because a weight
number without intake data cannot be explained or acted on.

**Independent Test**: Sign in, set a goal weight, record several weigh-ins across different dates,
and confirm the trend, the distance to goal, and the milestones — with no food entries in existence.

**Acceptance Scenarios**:

1. **Given** a person with a diet profile, **When** they record a weight for a date, **Then** it is
   stored against that date and becomes their current weight if it is the most recent reading.
2. **Given** a person who already recorded a weight for a date, **When** they record another weight
   for the same date, **Then** the existing reading is amended rather than duplicated.
3. **Given** several weigh-ins over time, **When** the person views their progress, **Then** they
   see the direction of travel over a recent window, the total change since they started, and the
   remaining difference to their goal.
4. **Given** a person whose goal is to lose weight, **When** a single reading is higher than the
   previous one but the trend is still downward, **Then** the product presents the trend as the
   headline rather than the single reading.
5. **Given** a person who reaches their goal weight, **When** they view their progress, **Then**
   the goal is shown as achieved and they are offered the chance to set a new goal.
6. **Given** a person recording a weight, **When** the date is in the future, **Then** the product
   rejects the entry and explains why.

---

### User Story 3 - Plan the week and shop for it (Priority: P3)

The person builds a plan for the coming days by assigning meals to occasions on a calendar. A meal
may be a single food or a saved recipe made of several ingredients. Once the plan exists, the
product tells them what the plan adds up to nutritionally per day and produces a consolidated
shopping list of everything the plan requires. When a planned day arrives, the person can accept the
plan as what they actually ate instead of logging it again by hand.

**Why this priority**: It converts the product from a record of the past into help with the future,
which is what changes behaviour. It is last because it depends on a food catalog and a target
having proven themselves in the first two slices.

**Independent Test**: Sign in, create a plan across several days, and confirm per-day nutrition
totals and a correctly consolidated shopping list — without relying on any historical food log.

**Acceptance Scenarios**:

1. **Given** a signed-in person, **When** they assign foods or recipes to meal occasions across a
   date range, **Then** the plan is saved and each planned day shows its projected nutrition totals
   against their daily target.
2. **Given** a saved recipe with several ingredients and a serving count, **When** it is placed in a
   plan, **Then** its per-serving nutrition contributes to that day's projection.
3. **Given** a plan spanning several days, **When** the person requests a shopping list, **Then**
   the same ingredient appearing in multiple meals is combined into one line with a summed quantity.
4. **Given** a planned day that has arrived, **When** the person confirms they ate the plan, **Then**
   the planned meals become actual logged entries for that day and are thereafter editable like any
   other entry.
5. **Given** a planned day the person did not follow, **When** the day passes without confirmation,
   **Then** the plan remains a plan and does not contribute to their actual intake history.
6. **Given** a plan whose projected daily energy is far below the person's target, **When** they view
   the plan, **Then** the product warns them before the day arrives rather than after.

---

### User Story 4 - Understand my habits (Priority: P4)

The product turns accumulated logs into observations: how consistently the person logs, how often
they land inside their target range, which days of the week they tend to overshoot, how their
average intake is trending, and the longest run of days they have stayed on target.

**Why this priority**: It is the highest-value slice that is genuinely optional — it needs history
to exist first, and the product is complete without it. It deliberately parallels the analytics the
product already offers on the quit-smoking side.

**Independent Test**: Seed a history of logged days, then confirm each reported statistic against a
hand-calculated expectation, including a period with gaps in logging.

**Acceptance Scenarios**:

1. **Given** a history of logged days, **When** the person views their habits, **Then** they see the
   proportion of days logged, the proportion of logged days within target, and the current and
   longest on-target streaks.
2. **Given** a history with days that were never logged, **When** statistics are calculated, **Then**
   unlogged days are reported as unlogged and are not counted as days of zero intake.
3. **Given** fewer than a meaningful number of logged days, **When** the person views trends, **Then**
   the product states there is not yet enough data rather than showing a misleading trend.

---

### Edge Cases

- A person changes their goal or body measurements mid-journey: past days keep the target that
  applied on the day they were logged, so history is not silently rewritten.
- A portion size of zero, or a negative one, is rejected.
- Absurd quantities (for example a portion thousands of times a normal serving) are rejected as
  likely typos rather than silently producing a nonsensical daily total.
- A person logs across a timezone change or near midnight: an entry belongs to the calendar day the
  person assigns it to, not to a server clock.
- A person edits a food they created after logging it: entries already logged keep the nutrition
  values that applied when they were recorded.
- A person deletes a food they created that is referenced by past entries or an active plan.
- A derived daily target would fall below a safe minimum for the person's profile.
- A person marks a date as both planned and already logged with conflicting content.
- A shopping list combines the same ingredient recorded in incompatible units.
- A recipe's serving count is changed after it has been placed in existing plans.
- The catalog contains no match for what the person searched for.
- A person with a diet profile has never logged anything: totals, trends and statistics must all
  present an empty state rather than zeros implying perfect adherence.

## Requirements *(mandatory)*

### Functional Requirements

**Profile and targets**

- **FR-001**: The system MUST let a signed-in person create a diet profile capturing the
  measurements, activity level, and goal needed to derive a daily nutrition target.
- **FR-002**: The system MUST derive a daily energy target and a macronutrient split from the
  profile, and MUST be able to explain the derivation to the person in plain language.
- **FR-003**: The system MUST allow a person to override the derived target with their own figure.
- **FR-004**: When a daily energy target — derived or manually entered — falls below a documented
  safe floor, the system MUST warn the person clearly, state that sustained intake below that level
  warrants professional guidance, and then allow them to proceed if they choose. The floors are
  fixed values, not derived ones, so that the warning can be explained in plain language.
- **FR-004a**: The system MUST record that a below-floor target was acknowledged, and MUST repeat
  the warning whenever such a target is changed rather than warning only once.
- **FR-005**: The system MUST record which target applied to each tracked day, so that changing a
  target later does not alter the assessment of days already logged.
- **FR-006**: The system MUST present itself as a self-tracking tool and MUST NOT present its
  targets or observations as medical or clinical advice.
- **FR-007**: The system MUST associate every profile, entry, weigh-in, plan and custom food with
  exactly one person, and MUST NOT expose one person's diet data to another.

**Food catalog**

- **FR-008**: The system MUST ship with a curated catalog of common foods, each carrying at least
  energy, protein, carbohydrate and fat values for a stated reference portion.
- **FR-009**: The system MUST let a person search the catalog by name and select a food from the
  results.
- **FR-010**: The system MUST let a person define their own food with its own nutrition values, and
  MUST make that food available to them for reuse.
- **FR-011**: The system MUST NOT allow one person's custom food to appear in another person's
  search results.
- **FR-012**: The system MUST capture the nutrition values in force at the moment an entry is
  recorded and keep them with that entry, so that editing or removing the underlying food afterwards
  changes nothing about days already logged. A person MUST therefore be free to edit or delete their
  own custom foods at any time without the system refusing on account of past entries.
- **FR-013**: The catalog MUST be present and identical on every fresh installation, and repeated
  restarts MUST NOT duplicate its contents.

**Daily logging**

- **FR-014**: Users MUST be able to record a food, a portion size, and a meal occasion against a
  specific calendar day.
- **FR-015**: Users MUST be able to amend and remove any entry they recorded.
- **FR-016**: The system MUST compute, for any day, the total energy and macronutrients consumed,
  the remaining allowance against that day's target, and the breakdown by meal occasion.
- **FR-017**: The system MUST reject entries dated in the future and entries dated before the
  person's diet profile began.
- **FR-018**: The system MUST reject non-positive portion sizes and portion sizes beyond a documented
  plausibility ceiling.
- **FR-019**: The system MUST distinguish a day with no entries from a day outside the tracked
  period, and MUST NOT report either as a day of zero intake.

**Weight and goals**

- **FR-020**: Users MUST be able to record a weight against a calendar date, and MUST be able to
  amend or remove it.
- **FR-021**: The system MUST hold at most one weight reading per person per calendar date; recording
  a second one for the same date amends the first.
- **FR-022**: The system MUST report the total change since tracking began, the change over a recent
  window, and the remaining difference to the goal.
- **FR-023**: The system MUST present trend over recent readings as the primary signal, and MUST NOT
  characterise progress on the basis of a single reading.
- **FR-024**: The system MUST recognise when a goal has been reached and offer the person the chance
  to set a new one.
- **FR-025**: The system MUST reject weight readings dated in the future, and readings outside a
  plausible human range.

**Planning**

- **FR-026**: Users MUST be able to assign foods or recipes to meal occasions on future dates.
- **FR-027**: Users MUST be able to define a recipe as a set of ingredient quantities plus a number
  of servings, and the system MUST derive the recipe's per-serving nutrition from its ingredients.
- **FR-028**: The system MUST project each planned day's nutrition totals and compare them to that
  person's daily target.
- **FR-029**: The system MUST produce a consolidated shopping list for a date range, combining
  repeated ingredients into a single summed line.
- **FR-030**: The system MUST list separately, rather than silently summing, ingredients whose
  quantities are recorded in units that cannot be combined.
- **FR-031**: Users MUST be able to convert a planned day into actual logged entries in one action,
  after which those entries behave like any other entry.
- **FR-032**: The system MUST keep plans and actual intake distinct: an unconfirmed plan MUST NOT
  contribute to intake history or habit statistics.

**Habits**

- **FR-033**: The system MUST report logging consistency, adherence to target, current and longest
  on-target streaks, and average intake over a recent window.
- **FR-034**: The system MUST exclude unlogged days from adherence calculations and report them as
  unlogged.
- **FR-035**: The system MUST decline to report a trend until enough data exists for the comparison
  to be meaningful.

**Boundaries**

- **FR-036**: The system MUST identify the person from their existing authenticated session; a
  person MUST NOT need a second account for diet features.
- **FR-037**: Diet data MUST be stored independently of the other subdomains' data, such that the
  diet capability can be deployed, migrated, and backed up on its own.
- **FR-038**: The system MUST function correctly for a person who has never used the other
  subdomains, and the other subdomains MUST continue to function if the diet capability is
  unavailable.
- **FR-039**: The system MUST collect the personal attributes it needs for target derivation — date
  of birth and biological sex among them — as part of diet setup, and MUST hold them within the diet
  capability's own store. It MUST NOT require the account record to be extended, and MUST NOT call
  another subdomain in order to complete setup.
- **FR-040**: The system MUST let a person choose their preferred unit system during setup and MUST
  present every weight, portion, and measurement in that system thereafter, while storing values in
  a single canonical system internally so that stored history is unaffected by changing the
  preference.

### Key Entities *(include if feature involves data)*

- **Diet Profile**: One person's tracked diet journey — when tracking began, their date of birth and
  biological sex, body measurements, activity level, unit preference, goal, and the daily target
  currently in force. The consistency boundary that owns their logged days and weigh-ins.
- **Daily Target**: An energy figure and macronutrient split, together with whether it was derived
  or manually set and the date from which it applied.
- **Food**: A named item with nutrition values for a reference portion. Either part of the shipped
  catalog and visible to everyone, or created by one person and visible only to them.
- **Logged Entry**: One food, one portion size, one meal occasion, on one calendar day, belonging to
  one person, carrying the nutrition values that applied when it was recorded.
- **Logged Day**: The set of entries for a person on a calendar date, with its totals and the target
  that applied that day.
- **Weigh-In**: One weight reading for one person on one calendar date; at most one per date.
- **Weight Goal**: A target weight and the direction of travel, with the date it was set and the
  date it was reached.
- **Recipe**: A named set of ingredient quantities plus a serving count, from which per-serving
  nutrition is derived. Owned by the person who created it.
- **Meal Plan**: An assignment of foods or recipes to meal occasions across a date range, with a
  state distinguishing planned from confirmed-as-eaten.
- **Shopping List**: A derived, consolidated view of everything a plan requires over a date range.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new person can go from signing in to seeing their first daily target in under two
  minutes without consulting help.
- **SC-002**: A person can record a food they have logged before in three interactions or fewer.
- **SC-003**: Day totals, remaining allowance, and macronutrient breakdown agree exactly with a
  hand calculation from the recorded entries, for every test scenario including partial portions.
- **SC-004**: Weight trend, total change, and distance to goal agree exactly with hand calculations
  across at least one scenario containing a plateau and one containing a reversal.
- **SC-005**: A shopping list for a plan containing the same ingredient in four separate meals
  produces exactly one line for that ingredient with the correct summed quantity.
- **SC-006**: 100% of the invariants stated in the Functional Requirements — every rejection case in
  FR-017, FR-018, FR-021, FR-025 and FR-030 — is covered by a test that proves the rejection, and
  the below-floor warning in FR-004 is covered by a test proving it warns without blocking.
- **SC-007**: Confirming a planned day produces logged entries identical to what manual logging of
  the same meals would have produced.
- **SC-008**: Habit statistics computed over a seeded 90-day history containing gaps match
  hand-calculated expectations, with unlogged days excluded from adherence.
- **SC-009**: Starting the diet capability against an empty store yields a working, populated food
  catalog with no manual step, and restarting it leaves the catalog unchanged in size.
- **SC-010**: The existing quit-smoking and account capabilities continue to pass their current
  tests unchanged after the diet capability is added.
- **SC-011**: A person signed in once can use diet features without a second authentication step.

## Assumptions

- The person is an adult self-tracking their own diet. Paediatric, clinical, and
  prescribed-therapeutic-diet use are out of scope.
- Existing authentication is reused; no new identity, registration, or password flow is introduced.
- The food catalog ships with the product and is curated rather than exhaustive. Coverage gaps are
  expected and are handled by the custom-food capability, not by an external lookup.
- Nutrition tracking covers energy plus the three macronutrients. Micronutrients, vitamins, water,
  fibre and sodium are out of scope for this feature.
- Barcode scanning, photo recognition of meals, and restaurant menu databases are out of scope.
- Social features — sharing plans, following others, coaching — are out of scope.
- One person has one active diet profile at a time.
- Unit system is a per-person preference set at diet setup, mirroring how the product already varies
  currency by person. Values are stored canonically and converted only for presentation.
- Data volume per person is small — on the order of a few entries per day — so no archival,
  pagination-at-scale, or aggregation-table strategy is assumed.
- The product remains single-language for this feature; food names are not translated.
