# Specification Quality Checklist: Exercise Logging

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-03
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- Items marked incomplete require spec updates before `/speckit-clarify` or `/speckit-plan`

### Validation iteration 1 — 2026-09-03

Four items failed, all traceable to three open decisions left undetermined by the source
description ("I need ability to log excercise dates for diet"): what an entry captures beyond a
date (FR-002), whether recorded exercise changes the day's calorie headroom (FR-014), and how it
relates to the activity level already declared in the plan (FR-015). Every other gap was closed
with a documented assumption.

### Validation iteration 2 — 2026-09-03

Decisions taken:

| ID | Decision | Rationale |
|--------|--------|--------|
| Q1 | The estimate is **shown**, the target is **untouched**, and the day's verdict is unchanged | Energy-burn estimates commonly overshoot by a wide margin. Converting one into extra calories hands a member a licence built on a guess — the most criticised behaviour in calorie-tracking apps, and a poor fit for a healthcare product. |
| Q2 | Activity and duration in whole minutes; intensity lives in the catalogue | "Running, easy" and "Running, fast" as separate activities is how published energy tables already work. It avoids asking members to self-rate something they rate badly, and keeps a field off every entry. |
| Q3 | Recorded exercise and the declared activity level stay **independent**, and the reason is shown | With nothing added to the target, the double-counting risk largely disappears. Independence is only confusing if unexplained, so FR-019 requires the explanation. |

**The decisive interaction.** Option B for Q1 — exercise earning calories back — would have broken
an existing guarantee: a logged day keeps the target that was in force when it was logged. Under B,
recording Tuesday's run on Thursday would retroactively flip Tuesday from over target to on target.
That is precisely the retroactive re-judging the diet feature's per-day target snapshot exists to
prevent, and it would have put this feature in direct conflict with a decision already made and
tested. **FR-015** and **SC-008** now state the guarantee explicitly so planning cannot lose it.

**Resulting changes:**

- Functional requirements grew from 26 to 30, sequential with no gaps. The relationship-to-eating
  group went from 3 requirements to 7, because "show it but change nothing" needs saying precisely:
  show the estimate (FR-014), do not move the target or the verdict (FR-015), do not present it as
  spendable (FR-016), derive and label it (FR-017), keep the activity level independent (FR-018),
  and explain why (FR-019).
- Success criteria grew from 11 to 13. **SC-008** verifies that a day's assessment survives exercise
  being added, edited or removed — including recorded days later. **SC-009** verifies no screen
  combines the estimate with the target into a spendable figure.
- Four acceptance scenarios and four edge cases added, covering the estimate, the unchanged verdict,
  back-dated logging, and an estimate that rounds to nothing.
- `Activity` now carries an energy rate, and `Exercise Entry` snapshots the estimate at the moment
  it was recorded — the same protection the food library already gives logged meals.

**Deliberately deferred to `/speckit-plan`:**

- The specific published table of activity energy costs. Recorded as an assumption; FR-017 states
  only that rate, duration and current weight determine the estimate.
- The breadth of the seeded activity catalogue, which is a content decision. SC-003 sets the bar.
- Letting logged exercise refine the declared activity level over time — a plausible later feature,
  excluded here because it needs a rule for how much history is enough, and this release is about
  recording activity rather than inferring from it.

**Status**: All 16 items pass. Ready for `/speckit-plan`.
