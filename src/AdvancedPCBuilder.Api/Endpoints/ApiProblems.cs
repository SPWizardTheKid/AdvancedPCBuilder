using Microsoft.AspNetCore.Http.HttpResults;

namespace AdvancedPCBuilder.Api.Endpoints;

/// <summary>The 400 and 404 ProblemDetails responses, built in one place so every endpoint looks the same.</summary>
public static class ApiProblems
{
    /// <summary>
    /// 400 with the errors per field, e.g. { "title": "One or more validation errors occurred.",
    /// "errors": { "cpuId": ["cpuId is required."] } }.
    /// </summary>
    public static ValidationProblem Validation(IReadOnlyDictionary<string, string[]> errors) =>
        TypedResults.ValidationProblem(errors);

    /// <summary>
    /// 404. The response is identical whether the ID does not exist at all or belongs to another
    /// tenant, so a client cannot learn which IDs other tenants have.
    /// </summary>
    public static ProblemHttpResult NotFound(string what, string id) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: $"{what} not found.",
            detail: $"{what} '{id}' was not found.");
}
