# Quickstart: Beer Days and Calendar Activity Markings

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

How to prove the feature works end to end. Assumes the diet programme is already set up for a member.

## Build and test

```bash
# from repo root
dotnet build OpenMind.Healthcare.sln
dotnet test OpenMind.Healthcare/backend/DietApi.Tests

cd OpenMind.Healthcare/frontend && npm run build
```

All three must be clean. The new backend tests:

| Test file | Proves |
|--------|--------|
| `Domain/BeerDayRulesTests.cs` | future date and pre-plan-start date each throw; a valid past date does not |
| `Domain/HabitAnalyserTests.cs` | beer / exercise counts, per-week rates, beer vs non-beer eating split, zero beer days, a sub-week period, a beer date outside the plan ignored |
| `Features/BeerDayHandlerTests.cs` | mark, mark-again idempotent, unmark, unmark-when-absent, future date → domain error, no plan → null, another member's day invisible, range excludes pre-plan and future |
| `Features/HabitInsightsHandlerTests.cs` | success path figures, no plan → null, unauthenticated throws |

## Apply the migration

```bash
cd OpenMind.Healthcare/backend/DietApi
dotnet ef database update          # or just start the service; Program.cs migrates on boot
```

Starting `DietApi` against an empty `diet.db` must produce a working schema (no seed for beer days —
they are member data).

## Manual walkthrough

1. **Mark a beer day.** Open **Diet → History**. Click a past date within the plan. In the popover,
   toggle **🍺 Beer day**. The cell shows the beer indicator (bottom-right). Reload — it persists.
2. **Both facts at once.** Pick a day that is over target and had exercise. Mark it as a beer day.
   The cell keeps its amber (over-target) fill, the exercise dot (top-right), and the beer bar
   (bottom-right) — all three visible (SC-002, SC-003).
3. **Unmark.** Reopen the popover, toggle **🍺 Beer day** off. The indicator clears.
4. **Future / pre-plan.** Confirm future dates and dates before the plan start cannot be marked.
5. **Year view.** Switch to the year view — beer and exercise indicators are still distinguishable at
   the smaller size, and the legend names both.
6. **Analytics.** Open **Diet → Analytics**. The **Habits** section shows beer days and exercise days
   for the period with per-week rates, and a comparison of eating outcomes on beer days versus other
   days. Change the period selector — the figures follow it. With no beer days logged, the section
   shows zero rather than disappearing (SC-004).

## API smoke test

```bash
TOKEN=...                       # a signed-in member's bearer token
BASE=http://localhost:5436/api

curl -s -H "Authorization: Bearer $TOKEN" "$BASE/beer-days?from=2026-08-01&to=2026-09-06"
curl -s -X PUT   -H "Authorization: Bearer $TOKEN" "$BASE/beer-days/2026-09-05"
curl -s -X DELETE -H "Authorization: Bearer $TOKEN" "$BASE/beer-days/2026-09-05"
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/diet-analytics/habits?period=Month"
```

New endpoints appear in the Scalar UI (`/scalar/v1` in development) under **BeerDays** and
**DietAnalytics**, and each returns `401` without the token.
