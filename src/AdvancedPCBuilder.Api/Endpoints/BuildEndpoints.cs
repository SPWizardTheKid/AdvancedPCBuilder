using AdvancedPCBuilder.Api.Contracts;
using AdvancedPCBuilder.Core.Application;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AdvancedPCBuilder.Api.Endpoints;

/// <summary>
/// /api/builds — saved builds of the current tenant (CRUD). Another tenant's build ID always
/// gives 404, exactly like an ID that does not exist; the data layer enforces this, not this class.
/// </summary>
public static class BuildEndpoints
{
    private const string Build = "Build";

    public static void MapBuildEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/builds").WithTags("Builds");
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static async Task<Ok<List<SavedBuildResponse>>> ListAsync(
        BuildService service, CancellationToken cancellationToken)
    {
        IReadOnlyList<SavedBuild> builds = await service.ListAsync(cancellationToken);
        return TypedResults.Ok(builds.Select(SavedBuildResponse.From).ToList());
    }

    private static async Task<Results<Ok<SavedBuildResponse>, ProblemHttpResult>> GetAsync(
        Guid id, BuildService service, CancellationToken cancellationToken)
    {
        SavedBuild? build = await service.GetAsync(id, cancellationToken);
        if (build is null)
        {
            return ApiProblems.NotFound(Build, id.ToString());
        }

        return TypedResults.Ok(SavedBuildResponse.From(build));
    }

    /// <summary>Validates, runs the compatibility check and saves the build with its report. 201 + Location header.</summary>
    private static async Task<Results<Created<SavedBuildResponse>, ValidationProblem>> CreateAsync(
        SaveBuildRequest? body, BuildService service, CancellationToken cancellationToken)
    {
        ValidationResult<SavedBuild> result = await service.CreateAsync(body?.Name, body?.Request, cancellationToken);
        if (!result.IsValid)
        {
            return ApiProblems.Validation(result.Errors);
        }

        SavedBuild build = result.Value!;
        return TypedResults.Created($"/api/builds/{build.Id}", SavedBuildResponse.From(build));
    }

    /// <summary>Replaces the name and request, re-runs the check. 404 is decided before validation.</summary>
    private static async Task<Results<Ok<SavedBuildResponse>, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id, SaveBuildRequest? body, BuildService service, CancellationToken cancellationToken)
    {
        ValidationResult<SavedBuild>? result = await service.UpdateAsync(id, body?.Name, body?.Request, cancellationToken);
        if (result is null)
        {
            return ApiProblems.NotFound(Build, id.ToString());
        }

        if (!result.IsValid)
        {
            return ApiProblems.Validation(result.Errors);
        }

        return TypedResults.Ok(SavedBuildResponse.From(result.Value!));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id, BuildService service, CancellationToken cancellationToken)
    {
        if (!await service.DeleteAsync(id, cancellationToken))
        {
            return ApiProblems.NotFound(Build, id.ToString());
        }

        return TypedResults.NoContent();
    }
}
