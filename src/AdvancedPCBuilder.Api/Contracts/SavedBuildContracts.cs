using AdvancedPCBuilder.Core.Application;

namespace AdvancedPCBuilder.Api.Contracts;

/// <summary>
/// Body of POST /api/builds and PUT /api/builds/{id}: { "name": "...", "request": { BuildRequest } }.
/// Both fields are nullable so a missing one becomes a 400 validation error from <see cref="BuildService"/>.
/// </summary>
public sealed record SaveBuildRequest(string? Name, BuildRequest? Request);

/// <summary>A saved build with the report of its latest check.</summary>
public sealed record SavedBuildResponse(
    Guid Id,
    string Name,
    BuildRequest Request,
    CompatibilityReportResponse Report,
    DateTimeOffset UpdatedAt)
{
    public static SavedBuildResponse From(SavedBuild build) => new(
        build.Id,
        build.Name,
        build.Request,
        CompatibilityReportResponse.From(build.Report),
        build.UpdatedAt);
}
