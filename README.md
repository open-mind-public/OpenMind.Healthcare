# OpenMind Health

A healthcare platform built as a set of independent **programmes**. Quitting smoking is one
programme, diet is another, and more are expected — so the architecture is shaped around adding
the next one without disturbing the last.

## Programmes

| Programme | What it does | Service | Routes |
|-----------|--------------|---------|--------|
| **Quit Smoking** | Smoke-free days, money saved, health milestones, relapse analytics, craving support | `QuitSmokingApi` | `/quit-smoking/*` |
| **Diet** | A calculated daily calorie and macro target, meal logging against a curated food library, streaks, weight tracking | `DietApi` | `/diet/*` |

Authentication is platform-level and shared: `UserApi` issues the token, and every programme
validates it with identical parameters, so one sign-in covers all of them.

## Architecture

**Backend** — one ASP.NET Core service per programme, following Domain-Driven Design with vertical
slices. Business rules live in aggregates and `IBusinessRule` implementations under `Domain/`;
handlers only orchestrate. Features are organised by use case, not by technical layer:

```
Features/<Feature>/
  <Feature>Endpoints.cs          # MapGroup("/api/<kebab-case>"), one per feature
  <Feature>Dtos.cs
  <UseCase>/<UseCase>Handler.cs  # command/query record + handler in one file
```

**Frontend** — a single Angular application acting as a platform shell. Programmes are declared in
one registry, [`src/app/programs/programs.ts`](OpenMind.Healthcare/frontend/src/app/programs/programs.ts),
which drives the programme switcher, the left navigation rail and the hub page. A programme knows
nothing about its siblings.

The rules the codebase is held to are written down in
[`.specify/memory/constitution.md`](.specify/memory/constitution.md). Decisions that outlive a
single feature are recorded in [`OpenMind.Healthcare/adrs/`](OpenMind.Healthcare/adrs/).

## Tech stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 10, ASP.NET Core Minimal APIs |
| Frontend | Angular 19 (NgModule-based) |
| Database | SQLite via EF Core 10 — one database file per service |
| Architecture | DDD building blocks, vertical slice features, bounded context per programme |
| Messaging | MediatR |
| Auth | JWT bearer, shared secret/issuer/audience across services |
| API docs | OpenAPI + Scalar |
| Tests | xUnit + Shouldly, in-memory fakes |
| Containers | Docker Compose |

## Development workflow

Non-trivial work follows [Spec Kit](https://github.com/github/spec-kit):

```
/speckit-specify → /speckit-clarify → /speckit-plan → /speckit-tasks → /speckit-analyze → /speckit-implement
```

Specs live in `specs/###-name/`. A spec describes behaviour and must not name a class, table or
endpoint; a plan describes construction. The diet programme was built this way and its artifacts —
specification, research decisions, data model, contracts, task list — are in
[`specs/001-diet-tracking/`](specs/001-diet-tracking/).
