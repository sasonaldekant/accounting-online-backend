using ERPAccounting.Application.Services;
using ERPAccounting.Infrastructure.Services;

namespace ERPAccounting.Infrastructure.Extensions;

/// <summary>
/// Registers infrastructure-level services in the DI container.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        services.AddMemoryCache();
        services.AddSingleton<CacheService>();
        services.AddScoped<ILookupService, StoredProcedureService>();
        services.AddScoped<IStoredProcedureService, StoredProcedureService>();
    }
}
