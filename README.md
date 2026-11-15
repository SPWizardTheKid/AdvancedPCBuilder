# AdvancedPCBuilder — Stage 2 prototype

A multitenant SaaS backend that checks whether a set of PC components is **physically and
electrically compatible**. The core module is a compatibility rule engine (rules R1–R8 from the
Stage 1 design, `docs/PSP_Kursinis_1etapas_v2.md` §3). Every check runs **all** rules and returns
one report:

```json
{
  "status": "Rejected",
  "requiredPsuWatts": 600,
  "issues": [
    { "rule": "R2-COOLER-HEIGHT", "severity": "Error",
      "message": "CPU cooler height 170 mm exceeds the case limit of 165 mm (over by 5 mm).",
      "actual": 170, "limit": 165, "difference": 5 },
    { "rule": "R5-PSU-POWER", "severity": "Error",
      "message": "Required PSU power 600 W exceeds the selected PSU power 450 W (short by 150 W).",
      "actual": 600, "limit": 450, "difference": 150 }
  ]
}
```

Status: any Error → `Rejected`; otherwise any Warning → `ApprovedWithWarnings`; otherwise `Approved`.

Two shops (tenants) use the same backend. Each has its own component catalog and its own saved
builds; one shop can neither read nor change the other shop's data. Isolation is enforced in the
data layer (EF Core query filters + a SaveChanges guard), not by the UI.

## Prerequisites

| Tool | Version | Required? |
|---|---|---|
| .NET SDK | **10.0.404** (pinned in `global.json`; any later 10.0.x SDK is also accepted) | Yes |
| Git | any | To clone |
| VS Code + C# Dev Kit | any | Optional (editor) |
| VS Code REST Client extension | any | Optional (to run `demo.http`) |

Nothing else is needed: the database is a SQLite **file** that is created automatically on the
first start. No database server, no Docker, no API keys.

Check your SDK: `dotnet --version` → `10.0.404` (or newer 10.0.x).

## Run

From the repository root:

```
dotnet run --project src/AdvancedPCBuilder.Api
```

The API listens on **http://localhost:5080**. Opening `http://localhost:5080` redirects to the
interactive API page `http://localhost:5080/scalar`.

On the first start the database `src/AdvancedPCBuilder.Api/advancedpcbuilder.db` is created and
seeded with the two tenants, their catalogs and one saved build each.

## Test

```
dotnet test
```

Expected: **95 tests, all passing** — 75 in `AdvancedPCBuilder.Core.Tests` (rules, engine, power
calculator, scenarios S1–S7, request validation) and 20 in `AdvancedPCBuilder.Api.Tests`
(HTTP integration and tenant isolation). The API tests use their own temporary SQLite file per
test class, so they never touch the demo database.

## Configuration

Everything is in `src/AdvancedPCBuilder.Api/appsettings.json`:

| Key | Default | Meaning |
|---|---|---|
| `ConnectionStrings:Default` | `Data Source=advancedpcbuilder.db` | SQLite file (relative to the working directory) |
| `PowerEstimates:MotherboardW` | 50 | Estimated motherboard power, W |
| `PowerEstimates:RamModuleW` | 5 | Per RAM module, W |
| `PowerEstimates:M2SsdW` / `SataSsdW` / `HddW` | 8 / 5 / 10 | Per storage drive, W |
| `PowerEstimates:FanW` | 3 | Per fan (cooler fans + case fans), W |
| `PowerEstimates:AioPumpW` | 5 | AIO pump, W |
| `PowerEstimates:SafetyMarginPercent` | 20 | `required = ⌈P × (100 + margin) / 100⌉` |
| `PowerEstimates:WarningHeadroomPercent` | 10 | R5 warns when less than this % of the PSU is left |

The app refuses to start if the connection string or the `PowerEstimates` section is missing
(otherwise every estimate would silently be 0 W). The power estimates are global, not per tenant.

**Reset the database:** stop the app, delete `src/AdvancedPCBuilder.Api/advancedpcbuilder.db`
(and `-shm`/`-wal` files if present), start again. It is re-created and re-seeded.

## HTTP API

Every `/api/*` request needs the header `X-Tenant-Id: techshop` or `X-Tenant-Id: pcmeistrai`.
Missing or unknown tenant → **401**.

| Method | Route | Result |
|---|---|---|
| POST | `/api/compatibility/check` | 200 report (also for `Rejected` builds — that is a business result); 400 invalid input |
| GET | `/api/components?type=Cpu` | 200 the tenant's components (`type` optional: Cpu, Motherboard, Ram, Gpu, Cooler, Case, Psu, Storage) |
| GET | `/api/components/{id}` | 200 / 404 |
| GET | `/api/builds` | 200 the tenant's saved builds |
| GET | `/api/builds/{id}` | 200 / 404 |
| POST | `/api/builds` | body `{ "name", "request" }` → 201 build with its report; 400 invalid |
| PUT | `/api/builds/{id}` | 200 re-checked build / 404 / 400 |
| DELETE | `/api/builds/{id}` | 204 / 404 |
| GET | `/health` | 200 (no tenant needed) |

Another tenant's ID always gives the same 404 as an ID that does not exist.

Request body for a check (required: `cpuId`, `motherboardId`, `ram`, `caseId`, `psuId`;
optional: `gpuId`, `cpuCoolerId`, `storage`, `caseFans` 0–10):

```json
{ "cpuId": "ts-cpu-65-am5", "motherboardId": "ts-mb-matx-am5",
  "ram": { "id": "ts-ram-ddr5-34", "quantity": 2 }, "gpuId": "ts-gpu-200-270",
  "cpuCoolerId": "ts-cool-air-155", "caseId": "ts-case-mid-360", "psuId": "ts-psu-600",
  "storage": [ { "id": "ts-ssd-m2-1tb", "quantity": 1 } ], "caseFans": 3 }
```

Ready-made requests for every demo step: [`src/AdvancedPCBuilder.Api/demo.http`](src/AdvancedPCBuilder.Api/demo.http)
(open in VS Code with REST Client and click "Send Request").

## Sample data (seeded on first start)

| Tenant ID | Name | Components | Saved build |
|---|---|---|---|
| `techshop` | TechShop UAB | 29, IDs start with `ts-` | `11111111-1111-1111-1111-111111111111` "Biuro PC" (scenario S1, Approved) |
| `pcmeistrai` | PC Meistrai MB | 10, IDs start with `pm-` | `22222222-2222-2222-2222-222222222222` "Dirbtuvių PC" (S1 with ATX board, Approved) |

techshop catalog (the IDs encode the important numbers):

| Type | IDs |
|---|---|
| CPU | `ts-cpu-65-am5`, `ts-cpu-120-am5`, `ts-cpu-170-am5` (AM5, 65/120/170 W), `ts-cpu-65-lga1700` (LGA1700, 65 W) |
| Motherboard | `ts-mb-matx-am5`, `ts-mb-atx-am5` (DDR5, 4 DIMM, 6 SATA, M.2 slots M2_1 [–], M2_2 [shares SATA 5,6]); `ts-mb-eatx-am5` (8 SATA, M2_3 shares SATA 7,8) |
| RAM | `ts-ram-ddr5-34`, `ts-ram-ddr5-56` (DDR5, 34/56 mm), `ts-ram-ddr4-32` (DDR4, 32 mm) |
| GPU | `ts-gpu-<W>-<mm>`: `200-270`, `220-308`, `220-309`, `425-300`, `250-280` |
| Cooler | air: `ts-cool-air-155` (1 fan), `-155-dual`, `-160-dual`, `-165-tower` (RAM clearance 32 mm), `-170` (height in mm; clearance 40 mm unless noted); AIO: `ts-cool-aio-240` (52 mm thick) |
| Case | `ts-case-mid-360` (ATX/mATX/ITX, cooler ≤ 165, GPU ≤ 360, front radiators 240/280/360), `ts-case-itx` (ITX only, cooler ≤ 160, GPU ≤ 330, no radiator) |
| PSU | `ts-psu-450`, `ts-psu-600`, `ts-psu-750` |
| Storage | `ts-ssd-m2-1tb` (M.2), `ts-ssd-sata-1tb` (SATA SSD), `ts-hdd-4tb` (HDD) |

pcmeistrai has the same models under `pm-` IDs: `pm-cpu-65-am5`, `pm-mb-atx-am5`, `pm-mb-eatx-am5`,
`pm-ram-ddr5-34`, `pm-gpu-200-270`, `pm-cool-air-155`, `pm-case-mid-360`, `pm-case-itx`,
`pm-psu-600`, `pm-ssd-m2-1tb`.

## Demo steps

1. `dotnet run --project src/AdvancedPCBuilder.Api` → open `http://localhost:5080/scalar`
   (or use `demo.http`). In Scalar, fill the `X-Tenant-Id` field of each request.
2. Tenant `techshop`, check **S7** (`demo.http` #3): `ts-cpu-170-am5`, `ts-mb-atx-am5`,
   `ts-ram-ddr5-34`×2, `ts-gpu-250-280`, `ts-cool-air-170`, `ts-case-mid-360`, `ts-psu-450`,
   `ts-ssd-m2-1tb`×1, 2 case fans → **Rejected**: R2 cooler 5 mm too tall AND R5 PSU short by
   150 W (required 600 W). Both errors are reported at once.
3. Swap the cooler to `ts-cool-air-155-dual` and the PSU to `ts-psu-750` (#4) → **Approved**,
   required 600 W.
4. Error cases: remove `cpuId` (#5) → **400** with `errors.cpuId`; or S4, an E-ATX board in a
   Mini-ITX case (#6) → **Rejected** with only R1-FORM-FACTOR.
5. Tenant isolation (#7–#15): with `X-Tenant-Id: pcmeistrai`,
   `GET /api/builds/11111111-1111-1111-1111-111111111111` → **404**; `PUT` and `DELETE` → **404**;
   then as `techshop` the same `GET` → 200, build "Biuro PC" unchanged. pcmeistrai using `ts-` IDs
   in a check → 400 "not found". No header / unknown tenant → 401.
6. `dotnet test` → all green. Test names read like the scenarios, e.g.
   `S7_TooTallCoolerAndWeakPsu_IsRejectedWithBothErrors`, `PcMeistrai_UpdateTechShopBuild_Returns404_AndBuildIsUnchanged`.

## Project structure

```
src/
  AdvancedPCBuilder.Core/            no package references (no EF, no ASP.NET)
    Domain/Components/               component spec records (CpuSpec, CaseSpec, AirCoolerSpec, ...)
    Domain/Build/                    BuildSpecification (the resolved build the rules see)
    Domain/Compatibility/            ICompatibilityRule, CompatibilityEngine, PowerCalculator, report types
    Domain/Compatibility/Rules/      one class per rule (R1 ... R8)
    Application/                     BuildRequest, BuildResolver (validation), CompatibilityCheckService,
                                     BuildService, ports IComponentCatalog / IBuildRepository / ITenantContext
  AdvancedPCBuilder.Infrastructure/  EF Core + SQLite: AppDbContext (tenant filters + guard), entities,
                                     EfComponentCatalog, EfBuildRepository, SeedData
  AdvancedPCBuilder.Api/             Minimal API: Program.cs, Endpoints/, Contracts/ (JSON DTOs),
                                     Tenancy/ (X-Tenant-Id middleware), demo.http
tests/
  AdvancedPCBuilder.Core.Tests/      pure unit tests: Rules/, Engine/, Scenarios/, Application/
  AdvancedPCBuilder.Api.Tests/       WebApplicationFactory tests: API, tenant isolation, DbContext isolation
docs/
  PSP_Kursinis_1etapas_v2.md         Stage 1 design (source of truth for the rules)
  STAGE2_PLAN.md                     implementation plan
  DECISIONS.md                       deviations from the Stage 1 design and why
  DEFENCE_NOTES.md                   code walkthrough: request flow, isolation, where to make changes
  AI_LOG.md                          what was AI-generated per phase
```

Dependencies point inward: Api → Infrastructure → Core, Api → Core. Core depends on nothing.

## Stage 3 scope (deliberately not in this prototype)

See `docs/DECISIONS.md` for the reasons.

- PostgreSQL (instead of SQLite) and EF Core migrations (instead of `EnsureCreated`)
- Docker / docker compose launch
- Redis cache and performance evidence (load tests)
- Authentication, login, users and roles (the tenant currently comes from the `X-Tenant-Id` header)
- Per-tenant configuration (custom margins, settings)
- React + TypeScript frontend
- AI build generator
