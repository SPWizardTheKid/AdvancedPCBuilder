using AdvancedPCBuilder.Api.Contracts;
using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Compatibility;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AdvancedPCBuilder.Api.Endpoints;

/// <summary>POST /api/compatibility/check — the core scenario (spec §3.2).</summary>
public static class CompatibilityEndpoints
{
    public static void MapCompatibilityEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/compatibility").WithTags("Compatibility");
        group.MapPost("/check", CheckAsync);
    }

    /// <summary>
    /// 200 with the report for every valid request, including Rejected builds (a business result).
    /// 400 only for invalid input (missing component, unknown ID, wrong type, bad quantity).
    /// </summary>
    private static async Task<Results<Ok<CompatibilityReportResponse>, ValidationProblem>> CheckAsync(
        BuildRequest? request, CompatibilityCheckService service, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ApiProblems.Validation(new Dictionary<string, string[]> { ["body"] = ["Request body is required."] });
        }

        ValidationResult<CompatibilityReport> result = await service.CheckAsync(request, cancellationToken);
        if (!result.IsValid)
        {
            return ApiProblems.Validation(result.Errors);
        }

        return TypedResults.Ok(CompatibilityReportResponse.From(result.Value!));
    }
}
