using ERPAccounting.Domain.Abstractions.Gateways;
using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Infrastructure.Data;
using ERPAccounting.Infrastructure.Repositories;
using ERPAccounting.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERPAccounting.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()));

        // Repositories, gateways, and unit of work required by application services
        RegisterPersistenceServices(services);

        return services;
    }

    private static void RegisterPersistenceServices(IServiceCollection services)
    {
        services.AddScoped<IStoredProcedureGateway, StoredProcedureGateway>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentLineItemRepository, DocumentLineItemRepository>();
        services.AddScoped<IDocumentCostRepository, DocumentCostRepository>();
        services.AddScoped<IDocumentCostItemRepository, DocumentCostItemRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}
