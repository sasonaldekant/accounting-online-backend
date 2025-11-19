using System;
using ERPAccounting.Application.Services;
using ERPAccounting.Infrastructure.Data;
using ERPAccounting.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERPAccounting.Infrastructure.Extensions;

/// <summary>
/// Registers infrastructure-level services in the DI container.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "DefaultConnection is not configured. Please define it in appsettings.json as described in the documentation.");
            }

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure();
            });
        });

        services.AddMemoryCache();
        services.AddSingleton<CacheService>();
        services.AddScoped<ILookupService, StoredProcedureService>();
        services.AddScoped<IStoredProcedureService, StoredProcedureService>();

        return services;
    }
}
