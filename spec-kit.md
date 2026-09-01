# Spec Kit in OpenMind.Healthcare

> **Installed here**: [github/spec-kit](https://github.com/github/spec-kit) (MIT), CLI `1.0.3.dev0`,
> Claude integration, PowerShell scripts. Configuration lives in
> [.specify/init-options.json](.specify/init-options.json).

---

## Overview
**Spec Kit** is an open-source toolkit from GitHub for implementing **Spec-Driven Development (SDD)**, especially when working with AI coding agents such as Copilot, Claude, Codex, etc. ([GitHub][1])

![Image](https://images.openai.com/static-rsc-4/ugiuglylExPCASsgbev3dHvvWysGPbcHynCRZ3VEdo0rSbgL9Rq3xW-uh6UiZ2MvgGKdusub6Snsin8VtRs0ikNLP80XBFdXb_Gc3n3Q5H7y56zm120D5JfEu8unBb81PvVZ9mqyUxYCVcmzcSorrhcMzW6BNksbVl_q_MMKlUsOFym7Y3EfvG2krRcoitMp?purpose=fullsize)

![Image](https://images.openai.com/static-rsc-4/2wrGoHXJ7RdlR4YdWZOedEXLTLKf3sll4923JbfwtON-Lpz2aTYI7JG61L2hwahLp6PH2UShODBKiZQ04UHkLEpwRaGP60UpKN2ig-nqkk0FtHFKOR7QgG6QF1Zo-QLIzAE-FeDSrWXPFsrvdoiivAyGLNahgw3A3D1NzGGyAsfV-juj__3HceXIeb0XY1Hk?purpose=fullsize)

### The basic idea

Traditional AI development often looks like:

> **Prompt → AI writes code → fix problems → prompt again**

Spec Kit changes this to:

> **Requirements → Specification → Technical Plan → Tasks → Code**

The core workflow is:

```text
Constitution
     ↓
Specify
     ↓
Clarify
     ↓
Plan
     ↓
Tasks
     ↓
Implement
```

Each stage produces structured artifacts that the next stage uses. ([GitHub][2])

### What each step does

**1. `/speckit.constitution`**

Defines project-wide principles.

For example:

```text
- Use Clean Architecture
- All business logic must be covered by unit tests
- APIs follow REST conventions
- Use PostgreSQL
- No direct database access from the UI
```

Think of this as the **engineering rules**.

---

**2. `/speckit.specify`**

You describe **what you want**, without telling the AI how to implement it.

For example:

```text
Build a payment service that allows customers
to create payments and receive notifications when
the payment succeeds or fails.
```

Spec Kit turns this into a detailed specification containing things like:

* User stories
* Functional requirements
* Acceptance criteria
* Edge cases
* Expected behavior

The important distinction is: **focus on WHAT and WHY, not technology.** ([GitHub][1])

---

**3. `/speckit.plan`**

Now you specify the **technical solution**.

For example:

```text
Use:
- .NET 10
- PostgreSQL
- Kafka
- DDD
- Clean Architecture
- Outbox Pattern
```

The AI produces the implementation/architecture plan.

---

**4. `/speckit.tasks`**

The plan gets converted into concrete development tasks:

```text
1. Create Payment aggregate
2. Create PaymentRepository
3. Create PaymentCreated event
4. Implement outbox table
5. Implement Kafka publisher
6. Add payment API
7. Add integration tests
```

---

**5. `/speckit.implement`**

The AI coding agent executes those tasks and writes the actual code.

So the important relationship is:

```text
          Human intent
               │
               ▼
        ┌─────────────┐
        │     Spec    │  ← What & Why
        └──────┬──────┘
               ▼
        ┌─────────────┐
        │     Plan    │  ← How
        └──────┬──────┘
               ▼
        ┌─────────────┐
        │    Tasks    │  ← Steps
        └──────┬──────┘
               ▼
        ┌─────────────┐
        │     Code    │
        └─────────────┘
```

### Why this is useful with AI

The key idea of SDD is that **the specification becomes the source of truth**, rather than code being the only source of truth. ([GitHub][3])

This is particularly useful for an experienced architect because you can spend more effort defining:

**business intent → requirements → constraints → architecture**

and let the AI handle much of:

**tasks → boilerplate → implementation → tests**

So, for example, instead of telling Claude:

> "Create a .NET microservice for payment processing using Kafka..."

you might start with:

> "Customers need to initiate a payment. The system must guarantee that a successful payment is published exactly once to downstream systems, even if the service crashes after committing the database transaction."

Then Spec Kit helps progressively turn that intent into a specification, architecture, tasks, and implementation.

**In short: Spec Kit is essentially a structured workflow/framework for making AI coding more specification-first instead of prompt-and-code-first.**

[GitHub Spec Kit repository](https://github.com/github/spec-kit?utm_source=chatgpt.com)

[1]: https://github.com/github/spec-kit?utm_source=chatgpt.com "GitHub - github/spec-kit: 💫 Toolkit to help you get started with Spec-Driven Development · GitHub"
[2]: https://github.com/github/spec-kit/blob/main/docs/reference/agentic-sdd.md?utm_source=chatgpt.com "spec-kit/docs/reference/agentic-sdd.md at main · github/spec-kit · GitHub"
[3]: https://github.com/github/spec-kit/blob/main/docs/concepts/sdd.md?utm_source=chatgpt.com "spec-kit/docs/concepts/sdd.md at main · github/spec-kit · GitHub"

## 1. What Spec Kit is

Spec Kit is a toolkit that puts a written, reviewable specification between a request and the code
that satisfies it. It is not a framework, a library, or a runtime dependency — nothing it installs
ends up in a build output. It is a set of **markdown templates**, **shell scripts**, and **prompt
files** that your AI coding agent reads.

Concretely, installing it into a repository adds three things:

- **Slash commands** for your agent (here: `.claude/skills/speckit-*/SKILL.md`) — each one is a
  prompt describing a phase of work.
- **Templates** (`.specify/templates/`) that define the shape of a spec, plan, and task list.
- **Scripts** (`.specify/scripts/powershell/`) that do the deterministic bookkeeping — allocating
  feature numbers, creating directories, resolving templates — so the agent does not have to.

The agent does the thinking. The scripts do the filing. The templates make the output consistent
enough to review.

---

## 2. The problem it solves

The default way to work with a coding agent is to describe what you want and let it write code. That
works until it doesn't, and it fails in four predictable ways:

| Failure | What it looks like |
|---|---|
| **Intent is not durable** | The reasoning for a design lives in a chat transcript that is gone next week. Six months later nobody knows why `FoodLogDay` is per-day instead of per-person. |
| **Ambiguity surfaces late** | "Build a diet tracker" hides a dozen decisions — where does date of birth live, what happens when a food is deleted after being logged. Unasked, the agent guesses, and you find out at code review. |
| **No shared definition of done** | Without stated acceptance criteria, "done" means "the agent stopped". Requirements silently go missing. |
| **Conventions erode** | Each new feature drifts from the last, because nothing wrote down what the conventions were. |

Spec-driven development front-loads the disagreement. You argue about a 400-line markdown document —
cheap to change — instead of about 6,000 lines of code that already have migrations attached.

**The honest trade-off**: this is overhead. For a bug fix or a one-file change it is pure cost. It
earns its keep on work that is large, long-lived, touches multiple layers, or will be maintained by
someone who was not in the room. A new bounded context qualifies. Renaming a variable does not.

---

## 3. The workflow

Seven commands, three of them optional. Run them in order; each reads what the previous one wrote.

```
  /speckit-constitution      once per project — the rules every feature must follow
          │
          ▼
  /speckit-specify     ──▶  specs/###-name/spec.md          WHAT and WHY, no technology
          │
          ▼
  /speckit-clarify     ──▶  resolves [NEEDS CLARIFICATION]  (optional, but do it)
          │
          ▼
  /speckit-plan        ──▶  plan.md, research.md,           HOW — the technical design
          │                 data-model.md, contracts/,
          │                 quickstart.md
          ▼
  /speckit-tasks       ──▶  tasks.md                        dependency-ordered work items
          │
          ▼
  /speckit-analyze     ──▶  consistency report               (optional — it finds real gaps)
          │
          ▼
  /speckit-implement   ──▶  actual code
```

### The commands

| Command | Purpose |
|---|---|
| `/speckit-constitution` | Create or amend the project constitution. Run once, amend rarely. |
| `/speckit-specify` | Turn a plain-language description into a specification. Allocates the feature number and directory. |
| `/speckit-clarify` | Asks up to 5 targeted questions about underspecified areas and writes the answers back into the spec. |
| `/speckit-plan` | Produces the technical design: research decisions, data model, API contracts, validation guide. |
| `/speckit-tasks` | Breaks the plan into dependency-ordered, individually checkable tasks grouped by user story. |
| `/speckit-analyze` | Cross-checks spec, plan and tasks for contradictions and uncovered requirements. Non-destructive. |
| `/speckit-checklist` | Generates a custom quality checklist for the feature. |
| `/speckit-implement` | Executes `tasks.md`. |
| `/speckit-converge` | Compares the codebase against the spec and appends the still-unbuilt work to `tasks.md`. Useful when implementation drifted or was interrupted. |
| `/speckit-taskstoissues` | Converts `tasks.md` into GitHub issues. |

### Where to stop

The natural review points are **after `/speckit-specify`** (is this the right product?) and **after
`/speckit-plan`** (is this the right design?). Both are cheap to change. After `/speckit-implement`
you are reviewing code and migrations, which is not.

---

## 4. The three document types, and why they are separate

This separation is the core idea. Getting it wrong is the most common way to misuse the tool.

### `spec.md` — behaviour

**Never names a class, table, package, or endpoint path.** It describes what a person can do and how
you would know it works. Written so a non-engineer could argue with it.

Contains: prioritised user stories (P1, P2, …), each independently testable; Given/When/Then
acceptance scenarios; numbered functional requirements (`FR-001`…); measurable success criteria
(`SC-001`…); edge cases; and stated assumptions.

Anything undecidable is marked `[NEEDS CLARIFICATION: question]` rather than silently assumed.

### `plan.md` + friends — construction

Where technology finally appears. Alongside it:

- **`research.md`** — every technical decision, each with its rationale *and the alternatives that
  were rejected*. This is the file that answers "why on earth did we do it this way" in a year.
- **`data-model.md`** — entities, value objects, invariants, persistence mapping.
- **`contracts/`** — the API surface: routes, payloads, status codes.
- **`quickstart.md`** — how to run it and the scenarios that prove it works.

### `tasks.md` — execution

Numbered, dependency-ordered, grouped by user story so each story stays independently shippable.
`[P]` marks tasks that can run in parallel. Every task names exact file paths.

---

## 5. Directory structure

```
OpenMind.Healthcare/
├── .specify/                          # Spec Kit's own files — commit these
│   ├── memory/
│   │   └── constitution.md            # ★ project principles, v1.0.0
│   ├── templates/                     # shapes for spec / plan / tasks / checklist
│   │   ├── spec-template.md
│   │   ├── plan-template.md
│   │   ├── tasks-template.md
│   │   ├── checklist-template.md
│   │   └── constitution-template.md
│   ├── scripts/powershell/            # deterministic bookkeeping
│   │   ├── create-new-feature.ps1     # allocates ###, creates dir, seeds spec.md
│   │   ├── setup-plan.ps1             # copies plan template into the feature
│   │   ├── setup-tasks.ps1            # reports which design docs exist
│   │   ├── check-prerequisites.ps1    # validates phase preconditions
│   │   ├── resolve-template.ps1       # composes template layers
│   │   └── common.ps1                 # Get-RepoRoot, Save-FeatureJson, Resolve-Template, …
│   ├── workflows/speckit/workflow.yml # bundled "Full SDD Cycle" with review gates
│   ├── integration.json               # which agent integration is installed
│   ├── init-options.json              # the choices made at init time
│   └── .gitignore                     # excludes feature.json (machine-local state)
│
├── .claude/skills/speckit-*/          # the ten slash commands, as prompt files
│
└── specs/                             # one directory per feature
    └── 001-diet-subdomain/
        ├── spec.md                    # what and why
        ├── plan.md                    # how
        ├── research.md                # decisions + rejected alternatives
        ├── data-model.md              # entities, invariants, persistence
        ├── contracts/diet-api.md      # HTTP surface
        ├── quickstart.md              # run it, prove it
        └── tasks.md                   # ordered work items
```

**Commit all of it.** `.specify/.gitignore` already excludes `feature.json`, which is a per-checkout
pointer to the feature you are currently working on. `.claude/skills/` holds only prompt text — no
credentials — so it is safe to track, and tracking it is what gives everyone the same commands.

---

## 6. This project's constitution

[`.specify/memory/constitution.md`](.specify/memory/constitution.md) is the highest-leverage file
here. Every `/speckit-plan` run evaluates itself against it twice — before research and after design
— and an unjustified violation blocks implementation.

It was written by reading the existing code, not by inventing process. Its six principles:

| # | Principle | In short |
|---|---|---|
| I | Bounded contexts own their data | Own project, own `DbContext`, own SQLite file. Only `UserId` crosses a boundary, and only from the JWT. |
| II | The domain model holds the rules | Rules live in aggregates and `IBusinessRule`, never in handlers or components. |
| III | Vertical slices, not layers | `Features/<Feature>/<UseCase>/` — command, handler and endpoint, not a layer cake. |
| IV | Time is a parameter | Every time-dependent method takes `DateTime? asOf = null`. This is what makes streaks testable. |
| V | Domain and slice tests are part of the feature | xUnit + Shouldly, hand-written fakes, no database. |
| VI | Migrations are checked in, seeds are idempotent | Guarded by `.Any()` so restarts cannot duplicate. |

Plus the architecture constraints a new subdomain must satisfy — the eight-point checklist covering
JWT config, CORS, Dockerfile, compose entry, port allocation, and the requirement that **both**
`proxy.conf.json` and `nginx.conf` are updated (editing one gives you a feature that works in
`npm start` and 404s in Docker).

Amend it with `/speckit-constitution`. It is semantically versioned; the Sync Impact Report at the
top of the file records what changed.

---

## 7. Using it

### Prerequisites

`uv` (already installed here — the CLI runs via `uvx`, so nothing is added to the project's
dependencies).

### Starting a feature

Just invoke the command with a description:

```
/speckit-specify Add a medication reminder subdomain so people can schedule doses and log adherence
```

That allocates the next number, creates `specs/002-.../`, seeds `spec.md` from the template, and
fills it in. Then:

```
/speckit-clarify          # answer the questions it asks
/speckit-plan             # design
/speckit-tasks            # work breakdown
/speckit-analyze          # catch gaps before writing code
/speckit-implement        # build it
```

### Re-running the CLI

```powershell
# Re-initialize or repair the installation (merges, does not clobber your specs)
uvx --from git+https://github.com/github/spec-kit.git specify init --here --force `
    --non-interactive --integration claude --script ps

# Check tooling and available integrations
uvx --from git+https://github.com/github/spec-kit.git specify check

# Upgrade the CLI itself
uvx --from git+https://github.com/github/spec-kit.git specify self --dry-run
```

### Adding it to another repository

```powershell
uvx --from git+https://github.com/github/spec-kit.git specify init my-project --integration claude
```

Spec Kit supports agents other than Claude — Copilot, Gemini, opencode and others — via
`--integration`. Run `specify check` to see what the installed version offers.

---

## 8. Gotchas

Things that cost time here, recorded so they don't cost it twice.

**The CLI's published documentation lags its interface.** The widely-cited `--ai claude` flag does
not exist in the version installed here; it is `--integration claude`. Likewise the commands are
`/speckit-constitution` (hyphen), not `/speckit.constitution`. Trust `specify init --help` over any
blog post, this document included.

**Non-interactive mode is mandatory for agent-driven installs.** Without `--non-interactive`, the
CLI opens an arrow-key menu that an agent harness cannot answer, and the install hangs.

**Newly installed skills need a restart.** Slash commands are loaded when Claude Code starts. If
Spec Kit was installed during your session, `/speckit-specify` will not appear in the menu until you
restart. The commands still work — they are plain prompt files that can be read and followed
directly.

**Version drift.** `git+https://…` resolves to whatever upstream HEAD is at that moment. This repo
installed `1.0.3.dev0`; HEAD moved to `1.0.4.dev0` shortly afterwards. If reproducibility matters,
pin a tag rather than tracking the default branch.

**Feature numbering is derived from `specs/`.** `create-new-feature.ps1` scans for the highest
`###-` prefix and adds one. Renaming or deleting a feature directory changes what the next feature
gets numbered.

**The scripts are PowerShell here** (`--script ps`, chosen for Windows). The `sh` and `py` variants
are functionally equivalent twins; if the team moves to CI on Linux, re-init with `--script sh`.

---

## 9. Worked example: `001-diet-subdomain`

The first feature specified this way is a complete reference. It produced roughly 2,100 lines of
design across seven documents before any code was written.

| Artifact | What to look at |
|---|---|
| [spec.md](specs/001-diet-subdomain/spec.md) | Four prioritised stories, 41 functional requirements, 11 measurable success criteria, and a `## Clarifications` section recording four decisions that were genuinely open. |
| [research.md](specs/001-diet-subdomain/research.md) | Twelve decisions, each with rejected alternatives — the energy formula, why logged entries snapshot their nutrition values, why aggregate boundaries diverge from `QuitJourney`. |
| [plan.md](specs/001-diet-subdomain/plan.md) | Both constitution gate evaluations, and a Complexity Tracking table admitting three deliberate deviations rather than hiding them. |
| [tasks.md](specs/001-diet-subdomain/tasks.md) | 124 tasks, dependency-ordered, with an honest note that the stories are independently *testable* but not independently *implementable*. |

**What the process actually caught**, before a line of code existed:

1. `QuitSmokingApi.Tests` is not registered in `OpenMind.Healthcare.sln` — so `dotnet test` at
   solution level silently runs nothing. The constitution's own quality gate was theatre.
2. `FR-006` (the requirement that the product not present itself as medical advice) had **no
   implementing task at all**. The traceability pass found it; it is now in T065.
3. `QuitSmokingApi`'s `https` launch profile binds port 3004, which is `UserApi`'s http port.
   Latent, but real.

None of those three are the kind of thing a code review finds. That is the argument for the
overhead, made concretely.

---

## 10. Further reading

- Repository: <https://github.com/github/spec-kit>
- The templates in [.specify/templates/](.specify/templates/) — reading `spec-template.md` is the
  fastest way to understand what a good spec contains.
- The skill prompts in [.claude/skills/](.claude/skills/) — each is a readable description of
  exactly what that phase does.
