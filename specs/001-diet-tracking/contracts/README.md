# Contracts: Diet Tracking

**Feature**: [../spec.md](../spec.md) | **Plan**: [../plan.md](../plan.md) | **Data model**: [../data-model.md](../data-model.md)

`DietApi` exposes one external interface: a JSON HTTP API consumed by the Angular front end. The
endpoint surface is [rest-api.md](./rest-api.md). This file holds the conventions every endpoint in
it obeys, so they are stated once rather than 20 times.

## Base paths and routing

Every group is mounted with `app.MapGroup("/api/<kebab-case>")` carrying `.WithTags(...)` and
`.RequireAuthorization()`, and each route adds `.WithName(...).WithOpenApi()` — the shape
Principle III mandates.

| Group | Base path | Feature folder |
|--------|--------|--------|
| Plan | `/api/diet-plan` | `Features/DietPlan` |
| Food log | `/api/food-log` | `Features/FoodLog` |
| Weight | `/api/weight` | `Features/Weight` |
| Food library | `/api/food-library` | `Features/FoodLibrary` |
| Statistics | `/api/diet-stats` | `Features/DietStats` |
| Achievements | `/api/diet-achievements` | `Features/DietAchievements` |
| Guidance | `/api/diet-guidance` | `Features/DietGuidance` |

The browser never calls these paths directly. It calls `/diet-api/...`, which both
`frontend/proxy.conf.json` (dev) and `frontend/nginx.conf` (container) rewrite to `/api/...` on
`DietApi` — the pattern `/user-api` already established. Adding one without the other makes the
feature work in dev and break in Docker.

## Authentication

Every endpoint requires a valid bearer token issued by `UserApi`. `DietApi` validates it with the
same `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`, and `ClockSkew = TimeSpan.Zero`, so one sign-in
covers every service (FR-042).

The acting member is read from the token's `NameIdentifier` claim through
`IUserService.GetCurrentUserId()`. **No endpoint accepts a user id in a route, query string, or
body** (FR-043). There is consequently no "get another member's plan" route to secure — the shape
of the API makes cross-member access unrepresentable rather than merely forbidden.

`GET /health` is the one unauthenticated route (R-013).

## Status codes

| Code | When |
|--------|--------|
| 200 | Success with a body |
| 201 | Plan created, with `Location` |
| 204 | Delete succeeded |
| 400 | `DomainException` — a broken business rule. Body: `{ "message": "..." }` |
| 401 | Missing or invalid token (framework-issued) |
| 404 | The member has no plan, or the addressed resource does not exist |

Endpoint delegates translate `DomainException` into `Results.BadRequest(new { message = ex.Message })`
and a missing resource into `Results.NotFound()`, and contain no other logic. Rule violations
surface as 400 with the rule's own `ErrorMessage`, so the message a member sees is the message the
domain wrote.

## Serialization

- `JsonStringEnumConverter` is registered, so `mealType`, `goal`, `activityLevel`, `state`, and
  every other enum cross the wire as names (`"Breakfast"`), never ordinals.
- `DateOnly` serializes as `"2026-09-02"`. `DateTime` is UTC ISO-8601.
- Calories are integers. Macronutrient grams and weights are decimals.
- Weights are kilograms and heights centimetres — always. Display conversion to pounds or feet is
  the client's job (R-012).

## Cross-cutting behaviours

**No plan yet**: every member-scoped endpoint outside `/api/diet-plan` and `/api/food-library`
returns 404 when the member has no plan. The client routes to setup.

**Empty states are not errors**: a member with a plan and no entries gets 200 with zeroed
statistics (FR-037); no weight readings gets 200 with an empty trend (FR-018). 404 means "no plan",
never "no data yet".

**Days are created and destroyed implicitly**: there is no create-day or delete-day endpoint.
Adding the first entry for a date creates the day; deleting the last entry destroys it and the date
returns to `NotLogged` (R-008).

**Snapshots**: an entry's nutrition and a day's target are captured at write time. A later
correction to the food library, or a later change to the plan's target, does not alter what a past
day returns (R-005, R-006).

## Validation the contract does not express

These are enforced in the domain and surface as 400. They are listed here so a consumer knows what
to expect rather than discovering them at runtime.

| Constraint | Bound |
|--------|--------|
| Plan start date | not in the future |
| Daily calorie target | greater than zero |
| Height | 50-250 cm |
| Age | 13-120 |
| Weight and target weight | 20-500 kg |
| Entry date | not future, not before plan start |
| Entry quantity | greater than zero |
| Entry calories | at most 10,000 kcal |
| Search results | capped at 20 |
