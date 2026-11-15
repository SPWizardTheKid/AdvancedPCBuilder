using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

namespace AdvancedPCBuilder.Api;

/// <summary>
/// Registers the Core module (rules, engine, application services) in DI. This lives in the Api
/// project because Core has no package references, not even Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class CoreServices
{
    public static IServiceCollection AddCore(this IServiceCollection services, PowerEstimates estimates)
    {
        // Global settings and stateless domain objects: one instance for the whole app.
        services.AddSingleton(estimates);
        services.AddSingleton<PowerCalculator>();

        // The rules, in report order. To add a rule: write the class and add ONE line here;
        // the engine receives all of them as IEnumerable<ICompatibilityRule> and does not change.
        services.AddSingleton<ICompatibilityRule, FormFactorRule>();
        services.AddSingleton<ICompatibilityRule, CoolerHeightRule>();
        services.AddSingleton<ICompatibilityRule, RadiatorMountRule>();
        services.AddSingleton<ICompatibilityRule, GpuLengthRule>();
        services.AddSingleton<ICompatibilityRule, SocketRule>();
        services.AddSingleton<ICompatibilityRule, PsuPowerRule>();
        services.AddSingleton<ICompatibilityRule, RamTypeRule>();
        services.AddSingleton<ICompatibilityRule, RamClearanceRule>();
        services.AddSingleton<ICompatibilityRule, M2SataSharingRule>();
        services.AddSingleton<CompatibilityEngine>();

        // Application services use the tenant's catalog/repository (scoped), so they are scoped too.
        services.AddScoped<BuildResolver>();
        services.AddScoped<CompatibilityCheckService>();
        services.AddScoped<BuildService>();

        return services;
    }
}
