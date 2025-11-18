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
    private const string DefaultInMemoryDatabaseName = "ERPAccounting";

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var databaseName = configuration.GetValue(
                "Infrastructure:InMemoryDatabaseName",
                DefaultInMemoryDatabaseName);

            options.UseInMemoryDatabase(databaseName);
        });

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentLineItemRepository, DocumentLineItemRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStoredProcedureGateway, StoredProcedureGateway>();

        return services;
    }
}
