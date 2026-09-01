<!--
SYNC IMPACT REPORT
Version change: (none) → 1.0.0
Rationale: Initial ratification. Principles were derived from the conventions already
practised in OpenMind.Healthcare (DDD building blocks, vertical slices, per-service SQLite
databases, asOf time injection, xUnit + Shouldly) rather than invented, so this codifies
existing practice instead of imposing new process.

Modified principles: none (initial adoption)
Added sections:
  - Core Principles I-VI
  - Architecture & Technology Constraints
  - Development Workflow & Quality Gates
  - Governance
Removed sections: none
Follow-up TODOs:
  - TODO(TEST_PROJECT_IN_SOLUTION): QuitSmokingApi.Tests exists on disk but is not
    referenced by OpenMind.Healthcare.sln. Principle V is unenforceable from a solution
    build until it is added. Track as a separate chore.
-->

# OpenMind.Healthcare Constitution

## Core Principles

### I. Bounded Contexts Own Their Data (NON-NEGOTIABLE)

Each subdomain is a separate ASP.NET Core project with its own `DbContext`, its own SQLite
database file, and its own migration history. A subdomain MUST NOT read another subdomain's
tables, share a `DbContext`, or declare a foreign key across a context boundary.

The only identifier that crosses a boundary is `UserId` (a `Guid`), and it MUST be read from
the authenticated JWT via `IUserService.GetCurrentUserId()` — never accepted from a request
body, route, or query string. When one context needs another's data, it goes over HTTP or
carries its own copy.

**Rationale**: `QuitSmokingApi` and `UserApi` are already independently deployable with
separate volumes in `docker-compose.yml`. Cross-context coupling would silently destroy that
property and make the databases impossible to migrate or scale independently.

### II. The Domain Model Holds the Rules

Business rules live in `Domain/` — in aggregates, value objects, business rules, and domain
services. They MUST NOT live in MediatR handlers, endpoint delegates, or Angular components.

- Aggregates derive from `AggregateRoot`, keep setters `private`, expose a `private`
  parameterless constructor for EF Core, and mutate only through intention-revealing methods
  (`Start`, `MarkDayAsSmoked`, `Update`) that call `SetUpdated()` and `Emit(...)`.
- Invariants are enforced with `CheckRule(new SomeRule(...))` implementing `IBusinessRule`,
  not with inline `if`/`throw`. Rules live in `Domain/Rules/` and name themselves via `nameof`.
- Value objects derive from `ValueObject`, are immutable, are constructed through a static
  factory (`Create`, `Zero`, `FromMinutes`), and implement `GetEqualityComponents()`.
- Collections inside an aggregate are exposed as `IReadOnlyCollection<T>` backed by a private
  field, and are mutable only through aggregate methods.

Handlers orchestrate: resolve the user, load the aggregate, call one domain method, persist,
map to a DTO. A handler that computes a business result is a defect.

**Rationale**: `QuitJourney` is the reference implementation. Every statistic, streak, and
analytic is derived inside the aggregate, which is why they are testable without a database.

### III. Vertical Slices, Not Layers

Features are organised by use case, not by technical role:

```
Features/<Feature>/
  <Feature>Endpoints.cs           # static class, Map<Feature>Endpoints extension method
  <Feature>Dtos.cs                # request/response records for the whole feature
  <UseCase>/<UseCase>Handler.cs   # command/query record + handler in ONE file
```

Endpoint groups MUST use `app.MapGroup("/api/<kebab-case>")` with `.WithTags(...)`,
`.RequireAuthorization()`, and per-route `.WithName(...).WithOpenApi()`. Endpoint delegates
translate `DomainException` into `Results.BadRequest(new { message = ex.Message })` and
missing resources into `Results.NotFound()`; they contain no other logic.

Commands and queries are `record` types implementing `IRequest<T>`. Handlers use primary
constructor injection.

**Rationale**: A new use case should be one new folder, one new file, and one new line in the
endpoints class — reviewable in isolation and deletable without archaeology.

### IV. Time Is a Parameter, Never an Ambient Fact

Every domain method whose result depends on the current time MUST accept `DateTime? asOf = null`
and default it internally (`asOf ?? DateTime.UtcNow`). All stored instants are UTC.
Calendar-day concepts use `DateOnly`, not `DateTime`.

**Rationale**: This is what makes `GetCurrentStreak`, `GetRelapseAnalytics`, and every
milestone calculation testable across arbitrary date ranges without freezing the system clock.
Diet tracking is even more date-sensitive (daily targets, weekly plans), so the pattern is
mandatory, not optional.

### V. Domain and Slice Tests Are Part of the Feature

Tests use xUnit with Shouldly assertions and mirror the source layout:
`Tests/Domain/<Behaviour>Tests.cs` for aggregate behaviour, `Tests/Features/<UseCase>Tests.cs`
for handlers. Handler tests use in-memory fakes from `TestSupport/` (a fake repository, a
`SignedInUser`, a builder) — not a real database and not a mocking framework.

Every new aggregate MUST ship with domain tests covering its invariants (each `IBusinessRule`
proven to throw) and its calculations at boundary values. Every new command handler MUST ship
with a slice test for the success path and the unauthenticated path.

**Rationale**: The existing suite proves the pattern works — `MarkingDaysAsSmokedTests` and
`StreakTests` cover genuinely tricky logic with no infrastructure.

### VI. Schema Changes Are Migrated and Seeds Are Idempotent

Schema changes ship as checked-in EF Core migrations. Every service applies
`context.Database.Migrate()` at startup inside a logged `try`/`catch` and then runs its
`DbInitializer`. Reference data seeding MUST be guarded (`if (!context.X.Any())`) so that
restarting a service never duplicates rows, and MUST call `SaveChanges()` once at the end.

Value objects owned by an aggregate are mapped with `OwnsOne`/`OwnsMany` and explicit column
names; `entity.Ignore(e => e.DomainEvents)` is required on every mapped entity.

**Rationale**: Containers restart. A seed that is not idempotent corrupts the catalog on the
second boot.

## Architecture & Technology Constraints

**Stack (fixed for all subdomains)**: .NET 10, ASP.NET Core Minimal APIs, MediatR 12,
EF Core 10 with SQLite, JWT bearer auth, OpenAPI + Scalar, Angular 19 (NgModule-based, not
standalone), Docker Compose.

**Every new subdomain service MUST provide**:

1. A `.csproj` referencing `Shared/DDD.BuildingBlocks`, registered in `OpenMind.Healthcare.sln`.
2. JWT validation configured identically to the existing services — same `Jwt:Secret`,
   `Jwt:Issuer`, `Jwt:Audience`, `ClockSkew = TimeSpan.Zero` — so one login works everywhere.
3. Its own `UserService`/`IUserService` registration and `AddHttpContextAccessor()`.
4. `JsonStringEnumConverter` registered so enums cross the wire as names, not ordinals.
5. A CORS policy allowing the Angular origins already listed in `QuitSmokingApi/Program.cs`.
6. A multi-stage `Dockerfile` matching the existing pattern: SDK build stage, non-root
   `appuser` runtime, `/app/data` volume for the SQLite file, health check, port 5000.
7. A `docker-compose.yml` service with its own named volume and a distinct host port.
8. A dev port in `launchSettings.json` that collides with nothing already allocated
   (3003 = QuitSmokingApi, 3004 = UserApi).

**Frontend routing to a new service MUST be added in both places or it will work in dev and
break in Docker**: `frontend/proxy.conf.json` (dev server) and `frontend/nginx.conf`
(container). Each service gets its own path prefix rewritten to `/api` — the pattern
established by `/user-api`.

**Port allocation is a shared resource.** Dev ports, container ports, and host ports MUST be
recorded in the feature plan before implementation begins.

## Development Workflow & Quality Gates

**Spec-driven flow.** Non-trivial work follows Spec Kit: `/speckit-specify` →
`/speckit-clarify` (when the spec carries `[NEEDS CLARIFICATION]` markers) → `/speckit-plan` →
`/speckit-tasks` → `/speckit-analyze` → `/speckit-implement`. Specs live in `specs/###-name/`.
Architectural decisions that outlive a single feature are additionally recorded in
`OpenMind.Healthcare/adrs/`.

**Specs describe behaviour, plans describe construction.** A spec MUST NOT name a class, table,
package, or endpoint path. Anything the spec cannot decide is marked
`[NEEDS CLARIFICATION: question]` rather than silently assumed.

**Gates before a feature is considered done**:

- `dotnet build OpenMind.Healthcare.sln` succeeds with no new warnings.
- `dotnet test` passes for every affected test project.
- `npm run build` succeeds in `frontend/`.
- New endpoints appear in the Scalar UI and require authorization.
- A migration exists for every model change, and starting the service against an empty
  database produces a working schema with seeded reference data.
- Constitution compliance is re-checked: any deviation is documented in the plan's Complexity
  Tracking section with a justification, or the deviation is removed.

## Governance

This constitution supersedes ad-hoc convention. Where this document and an existing file
disagree, this document wins and the file is a defect to be fixed.

**Amendment procedure**: Amendments are proposed as a change to this file, accompanied by an
updated Sync Impact Report comment at its head, and take effect once merged. An amendment that
invalidates existing code MUST name the migration path.

**Versioning policy** (semantic):

- MAJOR — a principle is removed or redefined such that existing compliant code becomes
  non-compliant.
- MINOR — a principle or section is added, or guidance is materially expanded.
- PATCH — clarification, wording, or typo fixes with no change in meaning.

**Compliance review**: Every plan produced by `/speckit-plan` MUST evaluate itself against the
Core Principles in its Constitution Check section, both before and after design. Complexity
that violates a principle is permitted only when the plan states what was tried instead and why
it was rejected. Unjustified violations block implementation.

**Version**: 1.0.0 | **Ratified**: 2026-09-01 | **Last Amended**: 2026-09-01
