# Decisions — deviations from the Stage 1 document

Every deliberate difference between `docs/PSP_Kursinis_1etapas_v2.md` (Stage 1) and the Stage 2 prototype.
This list feeds the Stage 3 "what changed and why" section.

| # | Stage 1 said | Stage 2 does | Why |
|---|---|---|---|
| D1 | PostgreSQL | SQLite file DB via EF Core | One-command start without installing a DB server; containerized launch (and PostgreSQL) is Stage 3 per requirements. EF Core keeps the switch small. |
| D2 | Redis cache | No cache | Cache only serves the performance quality attribute, which is proven in Stage 3. |
| D3 | Docker, `docker compose up` | `dotnet run` | Requirements move containerized launch to Stage 3. |
| D4 | React + TypeScript UI | API + Scalar page (+ optional single static HTML page) | Requirements: simple UI/API is enough. |
| D5 | AI build generator | Not implemented | Auxiliary feature, not the core module. Stage 3. |
| D6 | Tenant from authentication | Tenant from `X-Tenant-Id` header | Login/roles are Stage 3; isolation is still enforced in the data layer. |
| D7 | — | Each tenant has its own component catalog; specs stored as a JSON column | Specs are read whole by the engine and never queried by field; avoids 9 near-empty tables. |
| D8 | — | `EnsureCreated()` instead of migrations | Migrations will be introduced together with PostgreSQL in Stage 3. |
| D9 | .NET 8 | .NET 10 | .NET 8 reached end of support on 2026-11-10; .NET 10 is the current LTS. |
| D10 | — | `global.json` pins .NET SDK 10.0.404 (`rollForward: latestFeature`) | Reproducible builds: the dev machine also has a 10.0 preview SDK installed; the README can state one exact version. |
