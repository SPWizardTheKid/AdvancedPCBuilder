using AdvancedPCBuilder.Core.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdvancedPCBuilder.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the SQLite DbContext and the implementations of the Core ports.
    /// The caller must also register a scoped <see cref="ITenantContext"/> (the API does it per request).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IComponentCatalog, EfComponentCatalog>();
        services.AddScoped<IBuildRepository, EfBuildRepository>();
        return services;
    }
}
