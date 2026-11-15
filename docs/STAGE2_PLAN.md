# Stage 2 Implementation Plan — AdvancedPCBuilder prototype

> For the Claude Code agent. Read `CLAUDE.md` first (scope guardrails, conventions, workflow).
> Spec for every rule, formula and scenario: `docs/PSP_Kursinis_1etapas_v2.md` §3.
> Work phase by phase. After each phase: build + test green → tick boxes → AI_LOG entry →
> commit → STOP and explain to the student.

## 0. What Stage 2 is graded on (from the official requirements)

| Criterion | Points | What earns full marks | Where in this plan |
|---|---|---|---|
| Core function | 1.50 | Full scenario works input → result, business rules implemented, error/conflict handled. **If this fails, the whole stage = 0.** | Phases 1, 3 |
| Code structure & quality | 1.00 | Core logic separated from UI and infrastructure; clear names/responsibilities; no significant duplication | Architecture in `CLAUDE.md` |
| Automated tests | 0.50 | ≥3 meaningful core-module tests (normal, boundary/conflict, error) + ≥1 tenant isolation test; concrete assertions; all pass | Phases 1, 4 |
| Tenant data isolation | 0.50 | ≥2 tenants; one cannot read or modify the other's data; enforced by system logic, not UI | Phases 2, 4 |
| Run & README | 0.50 | Runs and tests per README; tools/versions, config, demo steps, sample data | Phase 5 |

Also required: everything pushed to the **same GitHub repo** that was submitted in Stage 1.
At the defence the student runs the prototype, shows the main scenario, an error case, tenant
isolation and test results, and must explain the code and make a small change live.

The requirements explicitly say these belong to **Stage 3** (do not build them now):
quality-attribute evidence, login and roles, per-tenant configuration, containerized launch.
A simple UI, API or CLI is enough; not all Stage 1 features are needed.

## 1. Deliberate deviations from the Stage 1 document (write these into `docs/DECISIONS.md` in Phase 0)

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

## 2. Solution layout

```
AdvancedPCBuilder.slnx
src/
  AdvancedPCBuilder.Core/
    Domain/
      Components/   ComponentSpec (abstract record) + CpuSpec, MotherboardSpec, M2SlotSpec, RamSpec,
                    GpuSpec, CoolerSpec (abstract) + AirCoolerSpec, AioCoolerSpec, CaseSpec, PsuSpec,
                    StorageSpec; enums FormFactor, MemoryType, StorageInterface
      Build/        BuildSpecification, StorageItem
      Compatibility/ ICompatibilityRule, CompatibilityEngine, CompatibilityIssue, CompatibilityReport,
                    CompatibilityStatus, Severity, RuleCodes, PowerCalculator, PowerEstimates
      Compatibility/Rules/  FormFactorRule, CoolerHeightRule, RadiatorMountRule, GpuLengthRule,
                    SocketRule, PsuPowerRule, RamTypeRule, RamClearanceRule, M2SataSharingRule
    Application/
      ITenantContext, IComponentCatalog, IBuildRepository
      BuildRequest (+ ComponentQuantity), BuildResolver, ValidationResult/Errors
      CompatibilityCheckService, BuildService, SavedBuild (application model)
  AdvancedPCBuilder.Infrastructure/
    AppDbContext, Entities/ (TenantEntity, ComponentEntity, SavedBuildEntity, ComponentType enum)
    ComponentSpecSerializer, EfComponentCatalog, EfBuildRepository, SeedData, DependencyInjection
  AdvancedPCBuilder.Api/
    Program.cs, Tenancy/ (HttpTenantContext, TenantResolutionMiddleware),
    Endpoints/ (CompatibilityEndpoints, ComponentEndpoints, BuildEndpoints), Contracts/ (DTOs),
    appsettings.json, Properties/launchSettings.json (http://localhost:5080), demo.http, wwwroot/ (Phase 6)
tests/
  AdvancedPCBuilder.Core.Tests/   Fixtures/TestComponents.cs, Rules/*, Engine/*, Scenarios/*, Application/*
  AdvancedPCBuilder.Api.Tests/    ApiFactory.cs, CompatibilityApiTests, TenantIsolationTests, DbContextIsolationTests
docs/  PSP_Kursinis_1etapas_v2.md, STAGE2_PLAN.md, DECISIONS.md, AI_LOG.md, DEFENCE_NOTES.md
README.md, .gitignore (dotnet + *.db), CLAUDE.md
```

## 3. Domain contract (core module)

```csharp
public interface ICompatibilityRule
{
    string Code { get; }                                   // e.g. RuleCodes.PsuPower = "R5-PSU-POWER"
    IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build);
}

public sealed record CompatibilityIssue(string RuleCode, Severity Severity, string Message,
                                        int? Actual = null, int? Limit = null, int? Difference = null);

public sealed record CompatibilityReport(CompatibilityStatus Status, int RequiredPsuWatts,
                                         IReadOnlyList<CompatibilityIssue> Issues);
// Status: any Error → Rejected; else any Warning → ApprovedWithWarnings; else Approved.

public sealed record BuildSpecification(
    CpuSpec Cpu, MotherboardSpec Motherboard, RamSpec Ram, int RamQuantity,
    GpuSpec? Gpu, CoolerSpec? Cooler, CaseSpec Case, PsuSpec Psu,
    IReadOnlyList<StorageItem> Storage, int CaseFanCount);
```

- `CompatibilityEngine(IEnumerable<ICompatibilityRule> rules, PowerCalculator power)` runs **every**
  rule, concatenates issues, computes status and `RequiredPsuWatts`.
- `PowerEstimates` (bound from `appsettings.json` section `PowerEstimates`, global — NOT per tenant):
  `MotherboardW=50, RamModuleW=5, M2SsdW=8, SataSsdW=5, HddW=10, FanW=3, AioPumpW=5,
  SafetyMarginPercent=20, WarningHeadroomPercent=10`.
- `PowerCalculator` is the **only** place power is summed:
  `P = cpu.TdpW + MotherboardW + RamModuleW×ramQty + gpu?.BoardPowerW + Σ storage(W×qty)
  + FanW×(cooler?.FanCount + caseFans) + (AIO ? AioPumpW : 0)`
  `required = (P × (100 + margin) + 99) / 100` (integer ceiling).
- Gpu and Cooler are optional (null → rules that need them are skipped, contribute 0 W).
  Air cooler and AIO are separate types; rules use pattern matching (`is AirCoolerSpec air`).

### Rules (exact; mirror spec §3.4)

| Class | Code | Logic | Severity |
|---|---|---|---|
| FormFactorRule | R1-FORM-FACTOR | `mb.FormFactor ∈ case.SupportedFormFactors` | Error |
| CoolerHeightRule | R2-COOLER-HEIGHT | Air only: `height ≤ case.MaxCoolerHeightMm`; diff = height − max | Error |
| RadiatorMountRule | R2b-RADIATOR-MOUNT | AIO only: `radiatorSize ∈ case.SupportedFrontRadiatorsMm` | Error |
| GpuLengthRule | R3-GPU-LENGTH | If GPU: `length ≤ case.MaxGpuLengthMm − thickness` (thickness = AIO radiator thickness else 0); limit = that value | Error |
| SocketRule | R4-SOCKET | `cpu.Socket == mb.Socket` | Error |
| PsuPowerRule | R5-PSU-POWER | `required > psu.Wattage` → Error (diff = required − wattage); else `required×100 > wattage×(100 − headroom)` → Warning | Error/Warning |
| RamTypeRule | R6-RAM-TYPE | type mismatch → Error; `ramQty > mb.DimmSlots` → Error (two separate issues possible) | Error |
| RamClearanceRule | R7-RAM-CLEARANCE | Air only: `ram.HeightMm ≤ air.RamClearanceMm`; diff = height − clearance | Error |
| M2SataSharingRule | R8-M2-SATA | m2Count > slots → Error (and stop this rule); assign M.2 drives to slots in order; disabled = union of SharedSataPorts of occupied slots; available = SataPorts − disabled; sataCount > available → Error; else disabled ≠ ∅ → Warning | Error/Warning |

`sataCount` = quantity of SataSsd + Hdd items; `m2Count` = quantity of M2 items.
Messages are English (CLAUDE.md; deviation from the Lithuanian spec examples recorded as D11), include the numbers,
e.g. `"CPU cooler height 170 mm exceeds the case limit of 165 mm (over by 5 mm)."`

## 4. Fixture catalog (single source of numbers — use in seed data AND in `Core.Tests/Fixtures/TestComponents.cs`)

Tenant **techshop** ("TechShop UAB") — IDs prefixed `ts-`:

| ID | Type | Values |
|---|---|---|
| ts-cpu-65-am5 | CPU | AM5, 65 W |
| ts-cpu-120-am5 | CPU | AM5, 120 W |
| ts-cpu-170-am5 | CPU | AM5, 170 W |
| ts-cpu-65-lga1700 | CPU | LGA1700, 65 W |
| ts-mb-matx-am5 | MB | mATX, AM5, DDR5, 4 DIMM, 6 SATA, M.2: M2_1 [], M2_2 [5,6] |
| ts-mb-atx-am5 | MB | ATX, AM5, DDR5, 4 DIMM, 6 SATA, M.2: M2_1 [], M2_2 [5,6] |
| ts-mb-eatx-am5 | MB | E-ATX, AM5, DDR5, 4 DIMM, 8 SATA, M.2: M2_1 [], M2_2 [], M2_3 [7,8] |
| ts-ram-ddr5-34 | RAM | DDR5, 34 mm |
| ts-ram-ddr5-56 | RAM | DDR5, 56 mm |
| ts-ram-ddr4-32 | RAM | DDR4, 32 mm |
| ts-gpu-200-270 | GPU | 270 mm, 200 W |
| ts-gpu-220-308 | GPU | 308 mm, 220 W |
| ts-gpu-220-309 | GPU | 309 mm, 220 W |
| ts-gpu-425-300 | GPU | 300 mm, 425 W |
| ts-gpu-250-280 | GPU | 280 mm, 250 W |
| ts-cool-air-155 | Air | 155 mm, RAM clearance 40 mm, 1 fan |
| ts-cool-air-155-dual | Air | 155 mm, RAM clearance 40 mm, 2 fans |
| ts-cool-air-160-dual | Air | 160 mm, RAM clearance 40 mm, 2 fans |
| ts-cool-air-165-tower | Air | 165 mm, RAM clearance 32 mm, 2 fans |
| ts-cool-air-170 | Air | 170 mm, RAM clearance 40 mm, 2 fans |
| ts-cool-aio-240 | AIO | radiator 240 mm, thickness 52 mm, 2 fans |
| ts-case-mid-360 | Case | ATX/mATX/Mini-ITX, cooler ≤165, GPU ≤360, front radiators [240,280,360] |
| ts-case-itx | Case | Mini-ITX only, cooler ≤160, GPU ≤330, front radiators [] |
| ts-psu-450 / ts-psu-600 / ts-psu-750 | PSU | 450 / 600 / 750 W |
| ts-ssd-m2-1tb | Storage | M2 |
| ts-ssd-sata-1tb | Storage | SataSsd |
| ts-hdd-4tb | Storage | Hdd |

Tenant **pcmeistrai** ("PC Meistrai MB") — smaller catalog, IDs prefixed `pm-`, same values as the
techshop equivalents: `pm-cpu-65-am5, pm-mb-atx-am5, pm-mb-eatx-am5, pm-ram-ddr5-34, pm-gpu-200-270,
pm-cool-air-155, pm-case-mid-360, pm-case-itx, pm-psu-600, pm-ssd-m2-1tb`.

Seeded saved builds (fixed GUIDs so demo/tests can reference them):
- techshop: `11111111-1111-1111-1111-111111111111` "Biuro PC" = scenario S1
- pcmeistrai: `22222222-2222-2222-2222-222222222222` "Dirbtuvių PC" = S1 with `pm-` IDs and ATX board

## 5. Scenario → test mapping (Core.Tests, all with exact expected numbers)

| Test | Input (techshop fixtures) | Expected |
|---|---|---|
| S1 normal | cpu-65, mb-matx, ram-34×2, gpu-200-270, cool-air-155, case-mid-360, psu-600, ssd-m2×1, caseFans 3 | Approved, P=345, required=414, no issues |
| S2 AIO + GPU at limit | cpu-120, mb-atx, ram-56×2, gpu-220-308, cool-aio-240, case-mid-360, psu-750, ssd-m2×1, caseFans 2 | Approved, P=425, required=510, no issues (R7 skipped for AIO) |
| S2-pair | same with gpu-220-309 | Rejected, R3 actual 309, limit 308, diff 1 |
| S3 PSU boundary | cpu-120, mb-atx, ram-34×2, gpu-425-300, cool-air-160-dual, case-mid-360, psu-750, ssd-m2×1, caseFans 2 | ApprovedWithWarnings, P=625, required=750, one R5 Warning |
| S3-pair | same but GPU power 426 W (test-local GpuSpec) | Rejected, required=752, R5 Error diff 2 |
| S4 form factor | cpu-65, mb-eatx, ram-34×2, gpu-200-270, cool-air-155, case-itx, psu-600, ssd-m2×1, caseFans 1 | Rejected, exactly one issue: R1 |
| S5 RAM clearance | cpu-120, mb-atx, ram-56×2, gpu-200-270, cool-air-165-tower, case-mid-360, psu-750, ssd-m2×1, caseFans 2 | Rejected, R7 actual 56, limit 32, diff 24; **no** R2 issue (165 ≤ 165) |
| S6a M.2/SATA error | cpu-65, mb-atx, ram-34×2, gpu-200-270, cool-air-155, case-mid-360, psu-750, ssd-m2×2, ssd-sata×2, hdd×3, caseFans 2 | Rejected, R8 Error (needs 5, available 4) |
| S6b M.2/SATA warning | same with ssd-sata×1, hdd×3 | ApprovedWithWarnings, one R8 Warning |
| S7 multiple errors | cpu-170, mb-atx, ram-34×2, gpu-250-280, cool-air-170, case-mid-360, psu-450, ssd-m2×1, caseFans 2 | Rejected, P=500, required=600, R2 diff 5 AND R5 diff 150 |
| S7-fixed (demo step 2) | S7 with cool-air-155-dual and psu-750 | Approved, required=600 |

Extra per-rule tests (small, one assertion focus each): R4 with cpu-65-lga1700; R6 with ram-ddr4-32;
R6 with ramQty 6 > 4; R2b with cool-aio-240 in case-itx; R8 with 3 M.2 drives on a 2-slot board;
engine status mapping (no issues / warning only / error+warning); PowerCalculator for S1 (345) and
margin change (e.g. 25 % → 432) to prove the setting is respected.

Application tests (with an in-memory fake `IComponentCatalog`): missing required component → validation
error; unknown ID → validation error; ID of wrong type (GPU id in `cpuId`) → validation error;
quantity < 1 → validation error. Validation errors mean rules are not executed.

## 6. HTTP API contract

All `/api/*` routes require header `X-Tenant-Id: techshop | pcmeistrai`. Missing or unknown → 401 ProblemDetails.

| Method | Route | Body | Result |
|---|---|---|---|
| POST | /api/compatibility/check | BuildRequest | 200 CompatibilityReport (incl. Rejected); 400 validation |
| GET | /api/components?type=Cpu | — | 200 list of tenant's components (id, name, type, specs) |
| GET | /api/components/{id} | — | 200 / 404 |
| GET | /api/builds | — | 200 tenant's saved builds |
| GET | /api/builds/{id} | — | 200 / 404 |
| POST | /api/builds | `{ name, request: BuildRequest }` | 201 saved build with its latest report; 400 validation |
| PUT | /api/builds/{id} | `{ name, request }` | 200 re-checked build / 404 / 400 |
| DELETE | /api/builds/{id} | — | 204 / 404 |
| GET | /health | — | 200 (no tenant needed) |

`BuildRequest` JSON (camelCase, enums as strings):
```json
{ "cpuId": "ts-cpu-65-am5", "motherboardId": "ts-mb-matx-am5",
  "ram": { "id": "ts-ram-ddr5-34", "quantity": 2 }, "gpuId": "ts-gpu-200-270",
  "cpuCoolerId": "ts-cool-air-155", "caseId": "ts-case-mid-360", "psuId": "ts-psu-600",
  "storage": [ { "id": "ts-ssd-m2-1tb", "quantity": 1 } ], "caseFans": 3 }
```
Required: cpuId, motherboardId, ram, caseId, psuId. Optional: gpuId, cpuCoolerId, storage (default []), caseFans (default 0, allowed 0–10).

## 7. Phases

### Phase 0 — Repository skeleton
- [x] `dotnet new` solution with the 3 src + 2 test projects from §2, all `net10.0`, project references per CLAUDE.md
- [x] `.gitignore` (standard dotnet + `*.db`, `*.db-shm`, `*.db-wal`)
- [x] `docs/DECISIONS.md` with D1–D9 from §1; `docs/AI_LOG.md` with the template in §8
- [x] Nullable enabled everywhere; `TreatWarningsAsErrors` in Core
- [x] `launchSettings.json` http profile on `http://localhost:5080`
- **Done when:** `dotnet build` and `dotnet test` succeed (empty test projects OK). Commit.

### Phase 1 — Core module: domain, rules, engine + unit tests (most important phase)
- [x] Component spec records and enums (§2, §4)
- [x] `BuildSpecification`, `StorageItem`, `PowerEstimates`, `PowerCalculator`
- [x] `ICompatibilityRule` + 9 rule classes (§3 table), `RuleCodes` constants
- [x] `CompatibilityEngine`, `CompatibilityReport`, status mapping
- [x] `Core.Tests/Fixtures/TestComponents.cs` mirroring §4 + a small fluent `TestBuild` helper
- [x] All tests in §5 (scenarios, pairs, per-rule, engine, power)
- **Done when:** all tests green; Core has zero package references. Commit.
- *If time runs very short:* R1–R5 + R7 are the minimum; R6 and R8 next. Never skip tests for a rule that exists.

### Phase 2 — Application layer + persistence + tenancy
- [x] Core/Application: `ITenantContext`, `IComponentCatalog` (`FindByIdsAsync`, `ListAsync(type?)`, `FindAsync(id)`), `IBuildRepository`
- [x] `BuildRequest`, `BuildResolver` (validation + mapping to `BuildSpecification`), `CompatibilityCheckService`, `BuildService` + application tests from §5
- [x] Infrastructure: `AppDbContext` with `TenantEntity`, `ComponentEntity (Id, TenantId, Type, Name, SpecJson)`, `SavedBuildEntity (Id, TenantId, Name, RequestJson, ReportJson, UpdatedAt)`
- [x] Global query filters on `ComponentEntity` and `SavedBuildEntity`: `e.TenantId == _tenant.TenantId`
- [x] `SaveChanges`/`SaveChangesAsync` guard: Added → TenantId must equal current (set it if empty); Modified/Deleted with another TenantId → throw `InvalidOperationException`
- [x] `ComponentSpecSerializer` (type discriminator → concrete spec record), `EfComponentCatalog`, `EfBuildRepository`
- [x] `SeedData`: `EnsureCreated()`, seed 2 tenants + catalogs + saved builds from §4 when DB empty. Seed each tenant through a DbContext created with that tenant's context (no filter/guard bypass hacks)
- **Done when:** build + tests green. Commit.

### Phase 3 — API
- [x] `HttpTenantContext` (scoped) + `TenantResolutionMiddleware` for `/api/*` (401 on missing/unknown tenant; checks Tenants table)
- [x] Endpoints from §6 in three static endpoint classes using `MapGroup`; handlers only call services
- [x] ProblemDetails for 400/401/404; JSON enums as strings; `PowerEstimates` bound from config
- [x] `AddOpenApi()` + `MapOpenApi()` + `MapScalarApiReference()` (development and default run)
- [x] `demo.http` with: S1 approved, S7 rejected, S7 fixed, validation error, tenant isolation calls
- [x] `public partial class Program;` for tests
- **Done when:** `dotnet run --project src/AdvancedPCBuilder.Api` starts, DB is created and seeded, demo.http calls return expected results. Commit.

### Phase 4 — Integration and isolation tests (Api.Tests)
- [x] `ApiFactory : WebApplicationFactory<Program>` that points `ConnectionStrings:Default` at a unique temp SQLite file per factory (delete on dispose)
- [x] `CompatibilityApiTests`: S1 via HTTP → 200 Approved, requiredPsuWatts 414; S7 → 200 Rejected with R2 and R5 codes; missing cpuId → 400
- [x] `TenantIsolationTests` (each a separate, clearly named test):
  - pcmeistrai `GET /api/components` contains no `ts-` IDs (and vice versa)
  - pcmeistrai `GET /api/builds/1111…` → 404
  - pcmeistrai `PUT /api/builds/1111…` → 404, then techshop `GET` shows the build unchanged
  - pcmeistrai `DELETE /api/builds/1111…` → 404, build still exists for techshop
  - pcmeistrai `POST /api/compatibility/check` using `ts-` component IDs → 400 "not found"
  - no header → 401; unknown tenant → 401
- [x] `DbContextIsolationTests`: context for tenant A sees only A's rows; attaching and modifying a B row through A's context throws
- **Done when:** all tests green. Commit.

### Phase 5 — README, docs, final verification
- [x] `README.md` (English): purpose; prerequisites (.NET 10 SDK, exact version from `dotnet --version`; VS Code + C# Dev Kit optional; REST Client extension optional); configuration (`appsettings.json`: connection string, PowerEstimates; no API keys needed in Stage 2); **one run command**; test command; tenants and sample data table; demo steps (§9); how to reset DB (delete `advancedpcbuilder.db`); project structure; "Stage 3 scope" list
- [x] `docs/DEFENCE_NOTES.md`: where each rule lives; how a request flows (endpoint → service → resolver → engine → rules); how isolation works (middleware → ITenantContext → query filter → SaveChanges guard); likely "small change" requests and exactly where to make them (see §10)
- [x] Fresh-clone check: clone into a temp folder, run the README commands verbatim, all tests pass, app starts
- **Done when:** `git status` clean, all boxes ticked. Commit. Tell the student to push.

### Phase 6 — OPTIONAL simple UI (only if Phases 0–5 are done and committed)
- [x] One `wwwroot/index.html` (vanilla JS + inline CSS, no build step), served via `UseDefaultFiles()` + `UseStaticFiles()`
- [x] Tenant dropdown (techshop / pcmeistrai, hardcoded), component dropdowns loaded from `/api/components?type=`, quantity inputs, case fans, "Check" button
- [x] Result panel: status badge, required PSU W, issues table (code, severity, message), response time in ms
- [x] Preset buttons "S1", "S7", "S7 fixed" to make the demo fast
- **Done when:** demo steps work through the page. Commit.
- Timebox: if it is not working within ~1 hour, revert and demo via Scalar/demo.http.

## 8. `docs/AI_LOG.md` entry template (agent fills the first three; student fills the last two)

```
## Phase N — <title> (<date>)
- Generated by Claude Code: <files / classes>
- Key decisions proposed: <...>
- Tests added: <count, names>
- Student review — kept / rewritten / rejected and why: <TODO student>
- How verified: <TODO student>
```

## 9. Demo script for the defence (put into README)

1. `dotnet run --project src/AdvancedPCBuilder.Api` → open `http://localhost:5080` (UI) or `/scalar`.
2. Tenant `techshop`: check S7 → Rejected: cooler 5 mm too tall, PSU short by 150 W (required 600 W).
3. Swap cooler to `ts-cool-air-155-dual` and PSU to `ts-psu-750` → Approved.
4. Error case: remove `cpuId` → 400 validation error; or S4 (E-ATX in Mini-ITX) → Rejected R1.
5. Isolation: with `X-Tenant-Id: pcmeistrai`, `GET /api/builds/11111111-…` → 404; `PUT` → 404; then as `techshop` the build is unchanged.
6. `dotnet test` → all green, show test names for S1–S7 and isolation.

## 10. Likely live-change requests (for DEFENCE_NOTES.md)

| Request | Where |
|---|---|
| Change safety margin 20 % → 25 % | `appsettings.json` → `PowerEstimates:SafetyMarginPercent` (test proves it) |
| Make GPU-length limit warn when < 5 mm left | `GpuLengthRule` + one test |
| Add a new rule (e.g. max case fans) | new class implementing `ICompatibilityRule` + one DI registration line + tests; engine unchanged (Open/Closed) |
| Add a third tenant | `SeedData` |
| Change a rule from Error to Warning | the rule class; status mapping is automatic in the engine |
