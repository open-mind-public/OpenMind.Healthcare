# Phase 0 Research: Diet Subdomain

**Feature**: `001-diet-subdomain` | **Date**: 2026-09-01

Every unknown carried by the Technical Context in [plan.md](./plan.md) is resolved here. Decisions
that outlive this feature are candidates for an ADR in `OpenMind.Healthcare/adrs/`.

---

## R1. Deriving a daily energy target

**Decision**: Mifflin-St Jeor for resting metabolic rate, multiplied by a discrete activity factor
to reach total daily energy expenditure, then adjusted by the person's goal rate.

```
RMR (male)   = 10 × kg + 6.25 × cm − 5 × age + 5
RMR (female) = 10 × kg + 6.25 × cm − 5 × age − 161

TDEE = RMR × activity factor
  Sedentary        1.2    little or no exercise
  LightlyActive    1.375  light exercise 1-3 days/week
  ModeratelyActive 1.55   moderate exercise 3-5 days/week
  VeryActive       1.725  hard exercise 6-7 days/week
  ExtraActive      1.9    physical job or twice-daily training

Target = TDEE ± (weekly rate in kg × 7700 kcal/kg ÷ 7)
```

7700 kcal per kilogram of body mass is the conventional planning figure. A goal rate of 0.5 kg per
week therefore shifts the target by 550 kcal per day.

**Rationale**: Mifflin-St Jeor is the formula the Academy of Nutrition and Dietetics recommends for
non-obese and obese adults, and it is more accurate than Harris-Benedict for contemporary
populations. It needs only weight, height, age and sex — exactly the attributes the clarified spec
puts on the diet profile (FR-039). It is pure arithmetic, so it belongs in the domain model and is
trivially testable against published worked examples.

**Alternatives considered**:

- *Harris-Benedict (1919, revised 1984)* — rejected: systematically overestimates for modern body
  compositions and needs the same inputs, so there is no cost saving.
- *Katch-McArdle* — rejected: more accurate but requires body-fat percentage, which the product has
  no way to obtain and which most people cannot supply reliably.
- *A flat table of targets by sex and goal* — rejected: trivially simple, but produces targets so
  crude that FR-002's "explain the derivation in plain language" would be an embarrassment.

**Consequence for the model**: activity factor and goal rate are enumerations plus a decimal, and
the calculation is a domain service or a value-object factory — never a handler.

---

## R2. Default macronutrient split

**Decision**: Default 30% protein / 40% carbohydrate / 30% fat by energy, converted to grams at
4 kcal/g protein, 4 kcal/g carbohydrate, 9 kcal/g fat. The person may override the percentages, and
the three MUST sum to exactly 100.

**Rationale**: This sits inside the Acceptable Macronutrient Distribution Ranges (protein 10-35%,
carbohydrate 45-65%, fat 20-35%) at the protein-forward end, which suits the satiety needs of
someone in an energy deficit. The Atwater factors are settled science and make the gram conversion
exact, which SC-003 depends on.

**Alternatives considered**:

- *40/30/30 or other fixed "zone" splits* — rejected: no better justified, and equally arbitrary.
- *No macro tracking, energy only* — rejected: the spec requires macronutrients throughout (FR-008,
  FR-016) and they are the main reason to log food rather than just weigh oneself.

**Edge case**: the 100% sum is a business rule with its own rule class, not a validation attribute.

---

## R3. Safe-floor values and warn-not-block behaviour

**Decision**: Fixed floors of **1200 kcal/day** for female profiles and **1500 kcal/day** for male
profiles. A target below the floor produces a warning that names the floor, states that sustained
intake below it warrants professional guidance, and requires explicit acknowledgement — then
proceeds (FR-004, FR-004a).

**Rationale**: These are the figures in common public-health guidance (NHS, Mayo Clinic) for the
level below which a diet is unlikely to meet micronutrient needs without supervision. Fixed numbers
can be stated in one sentence, which is what FR-004 asks for; a derived floor could not.

The warn-and-proceed choice was made explicitly by the requester. It respects an informed adult's
autonomy, and a hard block would be trivially defeated by simply under-logging — making the product
less safe by making its data less truthful.

**Alternatives considered**:

- *Hard block* — rejected by the requester; also encourages dishonest logging.
- *Floor as a percentage of RMR* — rejected: individually more accurate, but cannot be explained in
  plain language and multiplies the test surface.

**Consequence for the model**: the acknowledgement is state on the profile, not a UI-only concern,
because FR-004a requires the warning to recur whenever a below-floor target is changed.

---

## R4. Service topology and port allocation

**Decision**: A new `DietApi` service, sibling to `QuitSmokingApi` and `UserApi`.

| Concern | Existing | **DietApi** |
|---|---|---|
| Dev port (`launchSettings.json`) | 3003 QuitSmokingApi, 3004 UserApi | **3005** |
| Container port | 5000 | 5000 |
| Docker host port | 5431 api, 5434 user-api, 5435 ui | **5436** |
| SQLite file | `quitSmoking.db`, `users.db` | **`diet.db`** |
| Docker volume | `sqlite-data`, `user-sqlite-data` | **`diet-sqlite-data`** |
| Frontend path prefix | `/api`, `/user-api` | **`/diet-api`** |

**Rationale**: Constitution Principle I requires an independent store and migration history, and the
constitution's Architecture section requires ports to be allocated in the plan before implementation
begins. Every value above was checked against `docker-compose.yml`, both `launchSettings.json`
files, `proxy.conf.json`, and `nginx.conf`.

**Note on an existing collision**: `QuitSmokingApi`'s `https` launch profile binds
`https://localhost:3004`, which is `UserApi`'s http port. The two only clash if the `https` profile
is used, so it is latent rather than active. It is pre-existing, out of scope here, and worth a
separate chore — DietApi deliberately skips 3004 entirely.

---

## R5. Frontend routing for a third backend

**Decision**: Add `/diet-api` to **both** `frontend/proxy.conf.json` and `frontend/nginx.conf`,
following the `/user-api` pattern exactly — path prefix rewritten to `/api` at the proxy.

```jsonc
// proxy.conf.json
"/diet-api": {
  "target": "http://localhost:3005",
  "secure": false,
  "changeOrigin": true,
  "pathRewrite": { "^/diet-api": "/api" }
}
```

```nginx
# nginx.conf
location /diet-api/ {
    proxy_pass http://diet-api:5000/api/;
    # ...same proxy headers as the existing two blocks
}
```

**Rationale**: The constitution calls this out explicitly because editing only one of the two files
produces a feature that works in `npm start` and 404s in Docker — a failure that survives all local
testing. Both edits are separate, checkable tasks.

**Angular structure**: the app is NgModule-based, not standalone. Diet components are declared in
`app.module.ts` and routed with `canActivate: [AuthGuard]`, matching every existing protected route.
`AuthInterceptor` already attaches the bearer token to outgoing requests, so it covers `/diet-api`
with no change — worth an explicit verification task rather than an assumption.

---

## R6. Shape of the food catalog

**Decision**: Roughly 120 curated foods, seeded idempotently through a `DbInitializer`, storing
energy and the three macronutrients per 100 g (or per 100 ml for liquids), grouped into categories
(fruit, vegetable, grain, protein, dairy, fat, beverage, prepared). Catalog rows have no owner;
person-created foods carry an owner id and are filtered to that person (FR-011).

**Rationale**: Nutrition values for generic foods are facts, not creative expression, so composing a
modest generic catalog raises no licensing question. Per-100 g is the reference basis used by every
nutrition label, which keeps portion arithmetic to one multiplication and keeps SC-003 exact.

Seeding follows Principle VI: guarded by `if (!context.Foods.Any())` with a single `SaveChanges()`,
so container restarts cannot duplicate it (SC-009).

**Alternatives considered**:

- *USDA FoodData Central / Open Food Facts at runtime* — rejected by the requester at scoping; would
  add an outbound dependency, key management, and offline failure modes to a product that currently
  has none.
- *Importing a bulk dataset at build time* — rejected for now: tens of thousands of rows would bloat
  the repository and the SQLite file for no benefit at this stage, and it can be added later behind
  the same catalog abstraction without touching the domain.

---

## R7. Keeping logged history immune to later edits

**Decision**: Each logged entry stores a snapshot value object carrying the food's name and its
energy and macronutrient values as they stood at log time, alongside a reference to the food id for
provenance. Day totals are computed **only** from snapshots, never by re-reading the food.

**Rationale**: This is the clarified answer to FR-012. It makes custom foods freely editable and
deletable — the alternative forced confusing "cannot edit" walls onto a person's own data. At the
expected volume (a few entries per day per person) the duplication is irrelevant, and it removes an
entire class of "why did last month's totals change?" defect.

**Consequence**: the food id on an entry is a weak reference. It MUST be nullable or unenforced, so
that deleting a food never cascades into history. This is a deliberate departure from referential
integrity, justified by the requirement.

---

## R8. Aggregate boundaries

**Decision**: Four aggregate roots.

| Aggregate | Owns | Why separate |
|---|---|---|
| `DietProfile` | daily target, target history, safety acknowledgement, unit preference, weight goal, weigh-ins | One per person; the consistency boundary for "who this person is and what they are aiming at". Weigh-ins are owned because FR-021's one-per-date invariant must be enforced transactionally, exactly as `QuitJourney` owns `SmokedDay`. |
| `FoodLogDay` | the entries for one person on one date, and the target that applied | Loading a person's entire logging history to add one breakfast would not scale, and nothing in the spec requires cross-day invariants at write time. One aggregate per person per date keeps writes small and the one-day totals invariant local. |
| `Food` | its own nutrition values, ownership | Referenced by many entries but owned by none; catalog rows are shared. |
| `MealPlan` | planned meals over a date range, confirmation state | Independent lifecycle from logging: FR-032 requires plans and actual intake to stay distinct until explicitly confirmed. |

`Recipe` is an aggregate root as well, owning its ingredient lines.

**Rationale**: This mirrors how `QuitJourney` owns `SmokedDay` while `Achievement` and `CravingTip`
stand alone, so it needs no new mental model. The one deliberate divergence is `FoodLogDay`: unlike
`QuitJourney`, it is per-day rather than per-person, because entry volume grows without bound while
smoked days do not.

**Alternatives considered**:

- *One `DietJourney` aggregate owning everything, mirroring `QuitJourney` exactly* — rejected: every
  meal logged would load the person's entire history. The symmetry is superficially attractive and
  would not survive a year of use.
- *An entry as its own aggregate root* — rejected: the day's totals and the "belongs to the tracked
  period" rule (FR-019) have nowhere to live, and would leak into handlers, violating Principle II.

---

## R9. Units: canonical storage, converted presentation

**Decision**: Store canonically in metric — grams for mass, millilitres for volume, centimetres for
height, kilograms for weight. `UnitSystem` (Metric | Imperial) lives on the diet profile. Conversion
happens in the presentation mapping, never in the domain calculations.

```
kg → lb  × 2.20462        g → oz  ÷ 28.3495
cm → in  ÷ 2.54           ml → fl oz ÷ 29.5735
```

**Rationale**: FR-040 requires that switching the preference changes presentation only and never
recorded history — which is only achievable if the stored value is preference-independent. Keeping
conversion out of the domain means the Mifflin-St Jeor implementation has exactly one unit
convention to reason about, and it is the one the formula is defined in.

**Alternatives considered**:

- *Store in the person's chosen units with the unit tagged on each row* — rejected: every aggregate
  calculation would need conversion, and switching preference would either rewrite history or leave
  a mixed-unit table.

---

## R10. Combining quantities for the shopping list

**Decision**: A quantity is an amount plus a `MeasurementUnit` (Gram | Millilitre | Piece).
Consolidation groups by (food, unit) and sums within a group. Groups differing only by unit are
emitted as **separate lines** for the same food, never summed (FR-030).

**Rationale**: Grams and millilitres are not interchangeable without a density the product does not
have, and "2 pieces" of onion cannot be added to "150 g" of onion. Emitting two lines is honest and
still useful in a shop; silently summing would produce a wrong list, which is worse than a slightly
awkward one.

**Alternatives considered**:

- *Density table per food to normalise everything to grams* — rejected: needs data the catalog does
  not have and would produce confidently wrong numbers for anything not measured.
- *Refuse to mix units at plan time* — rejected: pushes an artificial constraint onto the person for
  the convenience of the shopping list.

---

## R11. Testing approach

**Decision**: A new `DietApi.Tests` project mirroring `QuitSmokingApi.Tests` — xUnit, Shouldly,
hand-written fakes in `TestSupport/`, no mocking framework, no database. Both the new test project
**and** the existing `QuitSmokingApi.Tests` are added to `OpenMind.Healthcare.sln`.

**Rationale**: Principle V requires domain and slice tests. The existing test project is not
currently referenced by the solution, so `dotnet build OpenMind.Healthcare.sln` does not compile it
and `dotnet test` at solution level runs nothing — which would make the constitution's own quality
gate vacuous. Fixing that is small, in scope, and a precondition for the gate to mean anything.

**Time control**: every time-dependent method takes `DateTime? asOf = null` per Principle IV, so
tests drive dates directly rather than manipulating the clock.

---

## R12. Cross-service impact

**Decision**: No change to `QuitSmokingApi`, `UserApi`, or `DDD.BuildingBlocks`. The diet capability
reads only the `UserId` claim from the shared JWT.

**Rationale**: SC-010 requires the existing capabilities to keep passing their tests unchanged, and
FR-038 requires each side to work without the other. Sharing the JWT secret, issuer and audience is
what makes SC-011 (no second sign-in) true without any coupling beyond configuration.

**Verification**: the JWT configuration block is duplicated, not extracted into
`DDD.BuildingBlocks`. Extracting it would be a genuine improvement, but it changes a shared library
that two working services depend on, which is a separate change with its own risk — noted as a
follow-up rather than smuggled into this feature.
