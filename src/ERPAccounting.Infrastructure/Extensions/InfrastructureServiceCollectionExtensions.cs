using System;
using ERPAccounting.Domain.Abstractions.Gateways;
using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Infrastructure.Data;
using ERPAccounting.Infrastructure.Repositories;
using ERPAccounting.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERPAccounting.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers infrastructure dependencies such as the DbContext, repositories and unit of work abstractions.
    /// </summary>
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

        services.AddScoped<IDocumentLineItemRepository, DocumentLineItemRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentCostRepository, DocumentCostRepository>();
        services.AddScoped<IDocumentCostItemRepository, DocumentCostItemRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStoredProcedureGateway, StoredProcedureGateway>();

        return services;
    }
}
