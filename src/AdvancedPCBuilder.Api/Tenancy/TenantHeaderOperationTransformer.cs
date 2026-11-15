using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AdvancedPCBuilder.Api.Tenancy;

/// <summary>
/// Documentation only: adds the required X-Tenant-Id header to every /api/* operation in the
/// OpenAPI document, so the Scalar page shows a header field for it. The header is actually
/// checked by <see cref="TenantResolutionMiddleware"/>, not by this class.
/// </summary>
public sealed class TenantHeaderOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        string? path = context.Description.RelativePath;
        if (path is not null && path.StartsWith("api/", StringComparison.Ordinal))
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = TenantResolutionMiddleware.HeaderName,
                In = ParameterLocation.Header,
                Required = true,
                Description = "Tenant ID: techshop or pcmeistrai.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                Example = JsonValue.Create("techshop")
            });
        }

        return Task.CompletedTask;
    }
}
