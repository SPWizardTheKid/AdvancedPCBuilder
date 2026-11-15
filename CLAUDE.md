# CLAUDE.md — AdvancedPCBuilder (university course project, Stage 2 prototype)

This file is loaded automatically in every session. Read it fully before doing anything.

## What this project is

A multitenant SaaS backend that checks whether selected PC components are physically and
electrically compatible. The **core module** is the compatibility rule engine (rules R1–R8).
This is a graded university course project ("Programų sistemų projektavimas"), Stage 2 of 3.
The student must be able to **explain and modify any line of code live** during the defence,
so simplicity and readability beat cleverness everywhere.

- Design spec (source of truth for rules, formulas, scenarios): `docs/PSP_Kursinis_1etapas_v2.md`
  (Lithuanian; section 3 is the core module).
- Implementation plan with phases and acceptance criteria: `docs/STAGE2_PLAN.md`.
- Stage 2 deadline: **2026-11-16** (live defence). Time is short: do exactly what the plan says.

## Tech stack (fixed — do not change without asking)

- .NET 10 (`net10.0`), C#, ASP.NET Core Minimal APIs
- EF Core 10 + **SQLite** (file DB, no install needed). PostgreSQL is Stage 3.
- xUnit for tests, `Microsoft.AspNetCore.Mvc.Testing` for API integration tests
- `Scalar.AspNetCore` for an interactive API page (on top of built-in `Microsoft.AspNetCore.OpenApi`)
- Optional (Phase 6): one static `wwwroot/index.html` page with vanilla JS. No React, no npm.

Allowed NuGet packages: the ones above plus `Microsoft.EntityFrameworkCore.Sqlite`,
`Microsoft.EntityFrameworkCore.Design` (only if needed), `Microsoft.Data.Sqlite`.
**Ask before adding any other package.** Specifically do NOT use: MediatR, AutoMapper,
FluentValidation, FluentAssertions, generic repository libraries, CQRS frameworks.

## STAGE 2 SCOPE — what to build

1. Core rule engine with rules R1, R2, R2b, R3, R4, R5, R6, R7, R8 exactly as specified in
   `docs/PSP_Kursinis_1etapas_v2.md` §3.4 (formulas, severities, integer arithmetic, `≤` at limits).
2. Validation of the request (missing required components, unknown IDs, wrong component type).
3. Persistence with SQLite: tenants, per-tenant component catalogs, per-tenant saved builds.
4. Basic multitenancy: 2 pre-seeded tenants, tenant resolved from the `X-Tenant-Id` header,
   isolation enforced by EF Core global query filters + a SaveChanges guard (not by the UI).
5. HTTP API: compatibility check, component listing, saved builds CRUD (needed to prove
   "cannot read or modify another tenant's data").
6. Automated tests: scenarios S1–S7 + paired boundary tests + per-rule tests + tenant isolation tests.
7. README (English) with tools/versions, config, ONE run command, test command, sample data,
   demo steps. Project must start with `dotnet run --project src/AdvancedPCBuilder.Api`.

## OUT OF SCOPE — Stage 3. Do NOT implement, scaffold, stub or "prepare" these

- Docker, Dockerfile, docker-compose, any containerization
- PostgreSQL, Redis, any caching layer, `IMemoryCache` optimisations
- Load/performance testing (k6, NBomber), benchmarks, performance evidence
- Authentication, login, JWT, API keys, users, roles, permissions
- Per-tenant configuration (custom margins, markups, settings per tenant)
- AI build generator / any LLM integration / API keys for AI services
- React/TypeScript frontend, npm, any JS build tooling
- Self-service tenant registration, cart, prices, markups, payments
- Rules marked out of scope in spec §3.4: PCIe bandwidth/lane counting, x16→x8 sharing,
  transient spike modelling, GPU power connectors, RAM speed/QVL, rear/top radiator mounting
- EF Core migrations (Stage 2 uses `EnsureCreated()`; migrations come with PostgreSQL)
- Cloud deployment, CI pipelines, mobile app

If something seems to require an out-of-scope item, STOP and ask the student. Do not leave
TODO stubs, empty interfaces or placeholder classes "for Stage 3".

## Architecture rules

```
src/AdvancedPCBuilder.Core            (no package references, no EF, no ASP.NET)
  Domain/       component specs, BuildSpecification, rules, engine, report, PowerCalculator
  Application/  ports (IComponentCatalog, IBuildRepository, ITenantContext), BuildRequest,
                BuildResolver (validation + mapping), CompatibilityCheckService, BuildService
src/AdvancedPCBuilder.Infrastructure  EF Core DbContext, entities, tenant filters, seed data,
                                      implementations of Core ports
src/AdvancedPCBuilder.Api             Minimal API endpoints, tenant middleware, DI wiring, wwwroot
tests/AdvancedPCBuilder.Core.Tests    pure unit tests of rules and engine (no DB, no HTTP)
tests/AdvancedPCBuilder.Api.Tests     integration tests (WebApplicationFactory + in-memory SQLite)
```

- Dependencies point inward: Api → Infrastructure → Core; Api → Core. Core depends on nothing.
- Each rule is its own class implementing `ICompatibilityRule` (Strategy pattern). The engine
  runs **all** rules and aggregates issues; it never stops at the first error.
- Endpoints are thin: parse → call an Application service → map result to HTTP.
- A rejected build is a business result: HTTP 200 with `status: "Rejected"`.
  Invalid input → 400 ProblemDetails. Missing/unknown tenant → 401. Other tenant's data → 404.
- Never reveal whether an ID exists in another tenant: "not found" looks identical either way.

## Coding conventions

- Identifiers, code comments and commit messages in **English**. User-facing rule messages
  (`CompatibilityIssue.Message`) in **English**, matching the spec examples.
- `README.md` in **English**.
- Prefer `record` / `sealed record` for immutable specs and results; `sealed class` for services.
- Nullable reference types on, warnings as errors in Core.
- Integer math only for mm and W. No `double` in rule calculations.
- Every rule class gets a short XML doc comment stating its formula and spec code (e.g. `R5-PSU-POWER`).
- Tests assert **rule codes, severities, statuses and numbers** (actual/limit/difference,
  requiredPsuWatts), not full message strings (at most one test checks a message).
- No duplicated logic: power calculation lives only in `PowerCalculator`.

## Workflow rules for the agent

- Work **one phase at a time** from `docs/STAGE2_PLAN.md`. At the end of each phase:
  run `dotnet build` and `dotnet test`, both must be green, tick the phase checkboxes in the plan,
  append an entry to `docs/AI_LOG.md`, commit with message `Phase N: <summary>`, then STOP and
  give the student a short explanation of what was built and which design decisions were made
  (so they can explain it at the defence). Wait for "continue".
- Do not `git push` unless the student asks.
- Any deviation from `docs/PSP_Kursinis_1etapas_v2.md` → record it in `docs/DECISIONS.md`
  (one short entry: what, why). This feeds the Stage 3 "what changed and why" section.
- If a spec number or scenario looks inconsistent, stop and ask; do not silently "fix" the spec.

## Commands

```
dotnet build
dotnet test
dotnet run --project src/AdvancedPCBuilder.Api      # http://localhost:5080
```
