using System.Collections.Generic;
using ERPAccounting.Application.Services;
using ERPAccounting.Domain.Abstractions.Gateways;
using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Infrastructure.Data;
using ERPAccounting.Infrastructure.Extensions;
using ERPAccounting.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERPAccounting.API.Tests.Services;

public class InfrastructureRegistrationTests
{
    [Fact]
    public void AddInfrastructureServices_UsesSqlServerProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=ERPAccountingDb;Trusted_Connection=True;MultipleActiveResultSets=true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.True(context.Database.IsRelational());

        var gateway = scope.ServiceProvider.GetRequiredService<IStoredProcedureGateway>();
        Assert.NotNull(gateway);
    }

    [Fact]
    public void AddInfrastructureServices_DoesNotRegisterLookupService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=ERPAccountingDb;Trusted_Connection=True;MultipleActiveResultSets=true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var lookupService = scope.ServiceProvider.GetService<ILookupService>();

        Assert.Null(lookupService);
    }

    [Fact]
    public void AddInfrastructureServices_RegistersRepositoriesAndUnitOfWork()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=ERPAccountingDb;Trusted_Connection=True;MultipleActiveResultSets=true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<DocumentRepository>(scope.ServiceProvider.GetRequiredService<IDocumentRepository>());
        Assert.IsType<DocumentLineItemRepository>(scope.ServiceProvider.GetRequiredService<IDocumentLineItemRepository>());
        Assert.IsType<DocumentCostRepository>(scope.ServiceProvider.GetRequiredService<IDocumentCostRepository>());
        Assert.IsType<DocumentCostItemRepository>(scope.ServiceProvider.GetRequiredService<IDocumentCostItemRepository>());
        Assert.IsType<UnitOfWork>(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }
}
