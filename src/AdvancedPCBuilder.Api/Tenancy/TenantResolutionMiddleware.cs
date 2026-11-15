using AdvancedPCBuilder.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AdvancedPCBuilder.Api.Tenancy;

/// <summary>
/// Resolves the tenant for every /api/* request from the X-Tenant-Id header (decision D6).
/// Missing header or a tenant that is not in the Tenants table → 401 ProblemDetails and the
/// request stops here. Otherwise the tenant is stored in <see cref="HttpTenantContext"/>, which
/// the DbContext query filters and SaveChanges guard use for the rest of the request.
/// Other paths (/health, /openapi, /scalar) do not need a tenant.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    public const string HeaderName = "X-Tenant-Id";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // Scoped services (tenant context, DbContext) are injected per request as method parameters.
    public async Task InvokeAsync(HttpContext context, HttpTenantContext tenant, AppDbContext db)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        string? tenantId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            await WriteUnauthorizedAsync(context, $"The {HeaderName} header is required.");
            return;
        }

        // The Tenants table has no query filter, so this works before the tenant is known.
        bool exists = await db.Tenants.AnyAsync(t => t.Id == tenantId, context.RequestAborted);
        if (!exists)
        {
            await WriteUnauthorizedAsync(context, $"Unknown tenant '{tenantId}'.");
            return;
        }

        tenant.SetTenant(tenantId);
        await _next(context);
    }

    private static Task WriteUnauthorizedAsync(HttpContext context, string detail) =>
        Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Tenant not resolved", detail: detail)
            .ExecuteAsync(context);
}
