using System.Text.Json.Serialization;
using AdvancedPCBuilder.Api;
using AdvancedPCBuilder.Api.Endpoints;
using AdvancedPCBuilder.Api.Tenancy;
using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration (appsettings.json). Fail at startup instead of silently using 0 W estimates. ----
string connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing in appsettings.json.");
PowerEstimates powerEstimates = builder.Configuration.GetSection("PowerEstimates").Get<PowerEstimates>()
    ?? throw new InvalidOperationException("Section 'PowerEstimates' is missing in appsettings.json.");

// ---- Services ----
builder.Services.AddCore(powerEstimates);
builder.Services.AddInfrastructure(connectionString);

// One tenant context per request; the DbContext gets it as ITenantContext, the middleware sets it.
builder.Services.AddScoped<HttpTenantContext>();
builder.Services.AddScoped<ITenantContext>(services => services.GetRequiredService<HttpTenantContext>());

// JSON: camelCase (default) and enums as strings ("Rejected", "Error", "Cpu").
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ProblemDetails (RFC 7807) bodies for errors the framework produces itself, e.g. malformed JSON → 400.
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi(options => options.AddOperationTransformer<TenantHeaderOperationTransformer>());

var app = builder.Build();

// ---- Database: create the SQLite file and seed both tenants on first start ----
using (IServiceScope scope = app.Services.CreateScope())
{
    var dbOptions = scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
    var engine = scope.ServiceProvider.GetRequiredService<CompatibilityEngine>();
    await SeedData.InitializeAsync(dbOptions, engine);
}

// ---- HTTP pipeline ----
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // In Development, malformed JSON throws BadHttpRequestException: keep its 400 instead of turning it into 500.
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError
});
app.UseStatusCodePages();          // ProblemDetails body for empty 4xx/5xx responses (e.g. unknown route → 404)
app.UseDefaultFiles();             // "/" → wwwroot/index.html (the simple demo page)
app.UseStaticFiles();              // serves files from wwwroot; no tenant needed for the page itself
app.UseMiddleware<TenantResolutionMiddleware>();   // /api/* only: X-Tenant-Id → 401 or tenant context

app.MapOpenApi();                  // /openapi/v1.json
app.MapScalarApiReference();       // /scalar (interactive API page)
app.MapHealthChecks("/health");

app.MapCompatibilityEndpoints();
app.MapComponentEndpoints();
app.MapBuildEndpoints();

app.Run();

/// <summary>Makes the generated Program class visible to WebApplicationFactory in Api.Tests.</summary>
public partial class Program;
