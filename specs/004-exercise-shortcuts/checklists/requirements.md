# Specification Quality Checklist: Exercise Shortcuts

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

No clarification markers. The one fork that would have needed asking — whether shortcuts are saved
deliberately or suggested from history — was settled before the spec was written, and the answer is
recorded in the Scope section as an explicit exclusion rather than left implicit.

Three decisions were taken as documented assumptions rather than questions, because each has a
defensible default and none changes the shape of the feature:

- **A shortcut records against the day in view, not always today.** Viewing today gives exactly the
  behaviour asked for; the same button then works for yesterday. Strictly more capable, no extra
  cost, and every existing date rule still applies.
- **Ten is the limit.** Past roughly that many, scanning the list costs more than typing the
  session and the feature stops being a shortcut.
- **No confirmation on tap.** A confirmation step would defeat the purpose; an accidental session is
  removed the way any other is.

The requirement worth re-reading before planning is **FR-010**: the estimate is computed when the
session is recorded, never held on the shortcut. Storing an estimate on the shortcut is the obvious
shortcut to take and would quietly freeze a member's weight at the moment they saved the button.
SC-003 is its gate.

**US1 deliberately covers both saving and tapping.** Split apart, neither half is independently
valuable — a shortcut you cannot tap and a tap with no shortcut are both nothing. The story is the
loop.
