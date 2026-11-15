# Defence notes — Stage 2

Cheat sheet for the live defence: where things are, how a request flows, how isolation works,
and exactly what to edit for the likely "make a small change" requests.

## 1. The system in five sentences

1. The **core module** is `CompatibilityEngine` + 9 rule classes in
   `src/AdvancedPCBuilder.Core/Domain/Compatibility/`. It has no idea about HTTP or databases.
2. A request contains component **IDs**; `BuildResolver` validates it and turns IDs into spec
   records (`BuildSpecification`) using the current tenant's catalog.
3. The engine runs **every** rule, collects all issues and derives the status
   (Error → Rejected, Warning → ApprovedWithWarnings, nothing → Approved).
4. A Rejected build is a normal business answer (HTTP 200). Only invalid input is HTTP 400.
5. Every database query is automatically limited to the tenant from the `X-Tenant-Id` header
   (EF Core global query filter), and every save is checked by a guard in `AppDbContext`.

## 2. Where each rule lives

All rule classes are in `src/AdvancedPCBuilder.Core/Domain/Compatibility/Rules/`, one class per rule,
each implementing `ICompatibilityRule` (`Code` + `Evaluate(BuildSpecification)`).
Codes are constants in `RuleCodes.cs`. Tests are in `tests/AdvancedPCBuilder.Core.Tests/Rules/<Class>Tests.cs`.

| Code | Class | Check (integer mm / W, `≤` allowed at the limit) | Severity |
|---|---|---|---|
| R1-FORM-FACTOR | `FormFactorRule` | board form factor ∈ case supported form factors | Error |
| R2-COOLER-HEIGHT | `CoolerHeightRule` | air cooler only: height ≤ case max cooler height | Error |
| R2b-RADIATOR-MOUNT | `RadiatorMountRule` | AIO only: radiator size ∈ case front radiator sizes | Error |
| R3-GPU-LENGTH | `GpuLengthRule` | GPU length ≤ case max GPU length − AIO radiator thickness (0 for air) | Error |
| R4-SOCKET | `SocketRule` | CPU socket == board socket | Error |
| R5-PSU-POWER | `PsuPowerRule` | required > PSU W → Error; required > 90 % of PSU → Warning | Error / Warning |
| R6-RAM-TYPE | `RamTypeRule` | RAM type == board type; module count ≤ DIMM slots (two separate issues) | Error |
| R7-RAM-CLEARANCE | `RamClearanceRule` | air cooler only: RAM height ≤ cooler RAM clearance | Error |
| R8-M2-SATA | `M2SataSharingRule` | M.2 count ≤ slots; SATA drives ≤ SATA ports left after M.2 sharing; sharing used → Warning | Error / Warning |

Rules that need an optional part (GPU, cooler) simply return no issues when it is missing.
Air cooler vs AIO is decided by type (`build.Cooler is AirCoolerSpec air`), not by a flag.

**Power** is calculated in exactly one place, `PowerCalculator`:
`P = CPU TDP + 50 (board) + 5 × RAM modules + GPU W + storage W + 3 × (cooler fans + case fans) + 5 (AIO pump)`,
`required = (P × 120 + 99) / 100` = P × 1.2 rounded **up**, in integers.

- S1: 65 + 50 + 10 + 200 + 8 (M.2) + 3 × (1 + 3) = **345 W** → 345 × 1.2 = **414 W**
- S7: 170 + 50 + 10 + 250 + 8 + 3 × (2 + 2) = **500 W** → **600 W** > 450 W PSU → short by 150 W

Both `PsuPowerRule` and the engine (for `requiredPsuWatts` in the report) call `PowerCalculator`.
The numbers 50/5/8/… come from `appsettings.json` → `PowerEstimates`, not from code.

## 3. How a check request flows

`POST /api/compatibility/check` with header `X-Tenant-Id: techshop`:

| Step | File | What happens |
|---|---|---|
| 1 | `Api/Tenancy/TenantResolutionMiddleware.cs` | Reads `X-Tenant-Id`; missing or not in the Tenants table → 401 and stop. Otherwise `HttpTenantContext.SetTenant("techshop")`. |
| 2 | `Api/Endpoints/CompatibilityEndpoints.cs` → `CheckAsync` | Thin: JSON body → `BuildRequest`, call the service, map the result to 200 or 400. |
| 3 | `Core/Application/CompatibilityCheckService.cs` | Resolve the request; if invalid return errors (rules are **not** run); else run the engine. |
| 4 | `Core/Application/BuildResolver.cs` | Required fields, quantities ≥ 1, case fans 0–10, loads all IDs in one call from `IComponentCatalog`, checks each ID exists and has the right type. Collects **all** errors. Builds `BuildSpecification`. |
| 5 | `Infrastructure/EfComponentCatalog.cs` | `IComponentCatalog` implementation: EF query on `Components`; the tenant filter is added automatically; JSON spec → record via `ComponentSpecSerializer`. |
| 6 | `Core/Domain/Compatibility/CompatibilityEngine.cs` → `Check` | `foreach` rule → `issues.AddRange(rule.Evaluate(build))`; status from severities; `RequiredPsuWatts` from `PowerCalculator`. |
| 7 | `Api/Contracts/CompatibilityReportResponse.cs` | Maps the Core report to JSON (`RuleCode` → `"rule"` as in spec §3.1). Core stays free of JSON attributes. |

Saved builds (`/api/builds`) go `BuildEndpoints` → `BuildService` → (`CompatibilityCheckService` for the
report) → `IBuildRepository` (`EfBuildRepository`). Every create/update re-runs the check, so a saved
build always stores the report for its current request.

**HTTP status rules:** Rejected build → 200. Invalid input → 400 ProblemDetails with errors per field
(`ApiProblems.Validation`). Missing/unknown tenant → 401. Not found **or another tenant's ID** → 404 with
identical body (`ApiProblems.NotFound`). On PUT, "not found" is checked before validation, so another
tenant's build is always 404, never 400.

**Dependency injection:** `Api/CoreServices.cs` (`AddCore`) registers PowerEstimates, PowerCalculator,
the 9 rules (registration order = order of issues in the report), the engine and the application
services. `Infrastructure/DependencyInjection.cs` registers the DbContext and the two repositories.
`Program.cs` wires everything, seeds the DB, and builds the pipeline.

## 4. How tenant isolation works (four layers)

1. **Middleware** (`TenantResolutionMiddleware`): no valid `X-Tenant-Id` → 401, the request never reaches
   an endpoint. The valid tenant ID is stored in the scoped `HttpTenantContext` (one per request).
2. **`ITenantContext` port** (`Core/Application/ITenantContext.cs`): `AppDbContext` receives the tenant
   through this interface, so the data layer does not depend on HTTP. Seeding and DB tests use
   `FixedTenantContext` instead.
3. **Global query filters** (`AppDbContext.OnModelCreating`): `HasQueryFilter(c => c.TenantId == CurrentTenantId)`
   on `Components` and `SavedBuilds`. EF adds `WHERE TenantId = @current` to **every** query, so code that
   forgets the tenant still cannot read another tenant's rows. Another tenant's build looks exactly like a
   missing one → 404.
4. **SaveChanges guard** (`AppDbContext.ApplyTenantGuard`): new rows get the current TenantId; adding a row
   for another tenant, or modifying/deleting a row whose original or new TenantId is not the current tenant,
   throws `InvalidOperationException`. Plus `SavedBuilds.TenantId` is a concurrency token, so the UPDATE/DELETE
   SQL itself contains `AND TenantId = @original`.

Component keys are `(TenantId, Id)`, so both tenants could even use the same component ID.
The UI/client is not trusted at all: isolation holds even if someone sends hand-made HTTP requests.

**Proof in tests:** `tests/AdvancedPCBuilder.Api.Tests/TenantIsolationTests.cs` (HTTP level: GET/PUT/DELETE of
techshop's build as pcmeistrai → 404 and unchanged, component lists, 401s) and `DbContextIsolationTests.cs`
(data level: filters, and the guard throws when the filter is bypassed on purpose).

## 5. Likely live-change requests

Run `dotnet test` after every change. Expected test fallout is listed so nothing surprises you.

### a) Change the safety margin 20 % → 25 %
- Edit `src/AdvancedPCBuilder.Api/appsettings.json` → `"SafetyMarginPercent": 25`. Restart the app.
- S1 now needs 345 × 1.25 = 431.25 → **432 W** (already proven by `PowerCalculatorTests.SafetyMarginFromSettings_IsRespected`).
- Core tests still pass (they pass their own estimates). `Api.Tests/CompatibilityApiTests` **fail on purpose**
  (they run the real config and expect 414 / 600; S7 becomes 625). Update the expected numbers or revert.
- Builds already saved in the DB keep their old report until they are updated (PUT re-checks).

### b) GPU-length rule: warn when less than 5 mm is left
In `GpuLengthRule.Evaluate`, replace the block `if (length <= limit) { yield break; }` with:

```csharp
if (length <= limit)
{
    int spare = limit - length;
    if (spare < 5)
    {
        yield return new CompatibilityIssue(
            Code,
            Severity.Warning,
            $"GPU fits with only {spare} mm to spare (limit {limit} mm).",
            Actual: length,
            Limit: limit);
    }
    yield break;
}
```

Add a test in `GpuLengthRuleTests` (e.g. GPU 357 mm in `CaseMid360` with an air cooler → one Warning).
Expected fallout: S2 (GPU exactly at 308) becomes ApprovedWithWarnings, and
`WithAirCooler_FullCaseLengthIsAvailable` now gets a warning — update both expectations; this is the
intended behaviour change. The engine needs no change: a Warning automatically gives ApprovedWithWarnings.

### c) Add a new rule (example: warn when no CPU cooler is selected)
1. New file `src/AdvancedPCBuilder.Core/Domain/Compatibility/Rules/CoolerPresentRule.cs`:
   ```csharp
   /// <summary>R9-NO-COOLER: warn when no CPU cooler is selected. Severity: Warning.</summary>
   public sealed class CoolerPresentRule : ICompatibilityRule
   {
       public string Code => "R9-NO-COOLER";

       public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
       {
           if (build.Cooler is null)
           {
               yield return new CompatibilityIssue(Code, Severity.Warning, "No CPU cooler selected.");
           }
       }
   }
   ```
   (Put the code into `RuleCodes` as a constant if there is time.)
2. One line in `src/AdvancedPCBuilder.Api/CoreServices.cs`:
   `services.AddSingleton<ICompatibilityRule, CoolerPresentRule>();`
3. Add `new CoolerPresentRule()` to the array in `tests/AdvancedPCBuilder.Core.Tests/Fixtures/TestEngine.cs`
   (unit tests build the engine without DI) and write a test `CoolerPresentRuleTests`.

`CompatibilityEngine` is **not** changed — that is the Open/Closed principle via the Strategy pattern.
A rule that needs a new spec field (e.g. "max case fans") also needs: the property on the spec record
(`CaseSpec`), its value in `SeedData` and `TestComponents`, and a DB reset (delete the .db file) because
the stored JSON specs were written without the new field.

### d) Add a third tenant
In `src/AdvancedPCBuilder.Infrastructure/SeedData.cs`: add a `TenantSeed` (ID, name, components with a new
ID prefix, one build) and add it to the `Tenants` array. Restart — no DB reset needed: seeding checks each
tenant separately and seeds only the missing one. Test with `X-Tenant-Id: <new id>`.

### e) Change a rule from Error to Warning
In the rule class, change `Severity.Error` to `Severity.Warning`. The status mapping in
`CompatibilityEngine.DetermineStatus` handles it automatically. Update the tests of that rule and any
scenario that expected `Rejected` because of it.

### f) Change a message text
Edit the interpolated string in the rule class. Only one test checks a message
(`CoolerHeightRuleTests.CoolerTallerThanCaseLimit_MessageContainsTheNumbers`); all others assert codes and numbers.

## 6. Likely questions

- **Why does the engine not stop at the first error?** The user should fix everything in one go (S7 shows
  two errors at once). Each rule is independent.
- **Why a class per rule?** Strategy pattern: each rule is small, testable alone, and new rules do not
  touch the engine.
- **Why integers only?** mm and W are whole numbers in the spec; `double` would give rounding surprises at
  the `≤` boundaries (e.g. 750 W required on a 750 W PSU must be allowed). The ceiling is done as `(x + 99) / 100`.
- **Why is Rejected a 200?** The request was valid and fully processed; "incompatible" is the answer, not an
  error. 400 means the question itself was wrong.
- **Why specs as a JSON column?** The engine always reads a whole spec and never queries by a field;
  one `Components` table instead of 9 near-empty ones (decision D7).
- **Why SQLite / no Docker / header instead of login?** Stage 2 requirements; PostgreSQL, Docker and
  authentication are Stage 3 (decisions D1, D3, D6 in `docs/DECISIONS.md`).
- **Why can't pcmeistrai learn that build 1111… exists?** The query filter makes it invisible, so the
  answer is the same 404 as for a random GUID.
- **What if the tenant header is forged?** In Stage 2 the header is trusted (no login). Isolation still
  means one tenant's requests can only touch that tenant's rows; tying the tenant to a login is Stage 3.

## 7. Demo checklist / troubleshooting

- Start: `dotnet run --project src/AdvancedPCBuilder.Api` → `http://localhost:5080/scalar` or `demo.http`.
- Demo order: S7 Rejected → S7 fixed Approved → missing cpuId 400 / S4 R1 → isolation 404s → `dotnet test`.
- "Address already in use": another instance is running — stop it (Ctrl+C in its terminal).
- Odd data after experiments: stop the app, delete `src/AdvancedPCBuilder.Api/advancedpcbuilder.db`, start again.
- `dotnet test` while the app is running is fine: tests use their own temporary database files.
