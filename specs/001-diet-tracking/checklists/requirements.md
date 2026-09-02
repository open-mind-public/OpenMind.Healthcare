# Specification Quality Checklist: Diet Tracking

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-02
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

### Validation iteration 1 — 2026-09-02

Four items failed, all traceable to three open decisions left undetermined by the source
description ("Build another subdomain for the current healthcare application. It is about
diet."): how members identify a food (FR-009), whether body weight was in scope (FR-006), and
whether the system recommends targets or only stores them (FR-005). No defensible default
existed for any of the three, so each was marked rather than assumed. All other gaps were
closed with documented assumptions.

### Validation iteration 2 — 2026-09-02

Decisions taken by the feature owner:

| ID | Decision | Effect on scope |
|--------|--------|--------|
| Q1 | Curated food library only; members select from a seeded catalogue and cannot define their own foods | Adds a reference catalogue with named serving sizes and a name search. Trustworthy nutrition values, at the cost of a member being unable to log a food the library lacks. |
| Q2 | Body-weight tracking is in scope — dated readings, a target weight, and a trend | Adds a second dated measurement series with its own history and goal, and supplies the current weight the target suggestion needs. |
| Q3 | The system suggests a daily target from body details and activity, and the member may override it | Requires body details at setup, a safe-minimum floor, an override warning, and a refresh-on-change rule that never overwrites a member's own choice silently. |

All three answers were propagated through the specification rather than substituted in place,
because they interact: Q3 depends on the body details Q2 introduces, and Q1 determines where an
entry's nutrition values come from and therefore what must be preserved when the library is
later corrected.

**Resulting changes:**

- User stories grew from five to six. Weight tracking entered as a new independently testable
  story at P4; achievements and guidance moved down to P5 and P6. Plan setup (P1) absorbed body
  details, the suggested target, and the override path. Meal logging (P2) now runs through
  library search and serving selection.
- Functional requirements grew from 28 to 44, sequential with no gaps. New groups cover body
  measurements (FR-012 to FR-018) and the food library (FR-019 to FR-022).
- Three requirements were added that none of the three questions asked for directly, but that
  the answers make necessary:
  - **FR-025** — an entry keeps the nutrition values in force when it was logged, so correcting
    a library food cannot retroactively change a day a member already saw assessed. Only
    relevant once nutrition values come from a shared catalogue (Q1).
  - **FR-009** — a refreshed suggestion is offered but never silently applied over a target the
    member set themselves. Without this, Q3 would let a weight reading quietly rewrite a
    deliberate choice.
  - **FR-007 / FR-008** — a safe-minimum floor on suggestions, and a warning (not a block) when
    a member overrides below it. Q3 moves the application from recording a member's number to
    proposing one, which carries a duty of care the earlier draft did not need.
- Success criteria grew from 9 to 13, adding suggestion acceptance rate (SC-002), search quality
  and speed (SC-004), assessment stability under library and target changes (SC-009), and safe
  floor enforcement (SC-010).
- Edge cases added for a missing library food, library corrections after the fact, serving-size
  equivalence and fractional quantities, two weight readings on one date, and an unsafe
  self-set target.

**Item-by-item findings:**

- *No [NEEDS CLARIFICATION] markers remain* — passes. Verified zero occurrences in spec.md.
- *Requirements are testable and unambiguous* — passes. All 44 requirements state a single
  verifiable behaviour, and each is covered by at least one acceptance scenario across User
  Stories 1-6 or by a stated edge case.
- *Scope is clearly bounded* — passes. Inclusions are settled by Q1-Q3. Exclusions are stated
  explicitly: member-defined foods, photo capture, barcode scanning, wearable import, recipe and
  meal-plan building, water intake, multiple concurrent plans, coach sharing, and cross-time-zone
  travel.
- *All functional requirements have clear acceptance criteria* — passes.
- *No implementation details leak* — passes. No class, table, endpoint, framework, or package is
  named anywhere in the specification, per the constitution's requirement that specs describe
  behaviour and plans describe construction.

**Deliberately deferred to `/speckit-plan`:**

- The specific resting-energy formula behind the target suggestion. Recorded as an assumption;
  the requirement states only that a standard published estimate adjusted for activity and goal
  is used.
- The safe-minimum calorie figures. Working defaults of 1,200 (women) and 1,500 (men) calories
  per day are recorded as an assumption and flagged for review before release.
- The residual half of the "day with no entries" question: FR-032 fixes the streak-relevant half
  (no entries means not logged), leaving only what happens when a member deletes a day's final
  entry. Called out as an edge case for the plan to settle.
- The breadth of the seeded food library at first release, which is a content decision rather
  than a behavioural one. SC-004 sets the bar it has to clear.

**Status**: All 16 items pass. Ready for `/speckit-plan`.

### Validation iteration 3 — 2026-09-02 (post-`/speckit-analyze` remediation)

Cross-artifact analysis after `/speckit-tasks` found three HIGH and four MEDIUM issues. All were
applied except the task-ordering finding, which the feature owner reviewed and elected to keep.
The specification changed as follows; all 16 checklist items still pass.

**Requirements added** (44 → 46, sequential, no gaps):

- **FR-045** — conflicting writes to the same logged day are detected and refused, not merged. The
  spec's concurrent-edits edge case had no coverage anywhere in the plan, data model, or tasks, and
  two design choices had quietly made it a live risk: `LoggedDay` being its own aggregate (R-004)
  and each day storing its own totals (R-010) together mean a last-write-wins update can persist one
  session's totals over another's entries.
- **FR-046** — a plan's last remaining weight reading cannot be deleted. FR-017 makes the newest
  reading the current weight for target suggestions, and nothing had stopped a member deleting the
  only one, which would have left the suggestion path with no input.

Both carry acceptance scenarios (US2 scenario 13, US4 scenario 7) so they are testable rather than
merely asserted.

**Requirements clarified**:

- **FR-025** now states what happens when a *member* edits an entry, as distinct from a background
  library correction: values are re-read and re-snapshotted. The accepted consequence — a
  quantity-only edit also picks up a corrected nutrition value — is recorded in research.md R-005.
- **SC-004** now names a fixed judging corpus. "85% of searches return a usable match" was
  unfalsifiable without one.

**Success criteria reclassified**: SC-002 and SC-013, plus the adoption half of SC-003 (now
**SC-014**), moved to a new *Post-Launch Outcome Measures* section. All three need usage
instrumentation this release does not build, so presenting them among the release gates overstated
what the feature verifies. SC-003 keeps its verifiable half — the 20-second timing.

**Not changed, deliberately**: task ordering places domain tests after the code they cover, which
differs from the tasks template's test-first rule and from `/speckit-implement`'s stated behaviour.
Raised in analysis, reviewed, and kept at the feature owner's direction.
