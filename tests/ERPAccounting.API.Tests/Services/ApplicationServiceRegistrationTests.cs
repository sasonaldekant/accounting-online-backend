using ERPAccounting.Application.Extensions;
using ERPAccounting.Application.Services;
using ERPAccounting.Domain.Abstractions.Gateways;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ERPAccounting.API.Tests.Services;

public class ApplicationServiceRegistrationTests
{
    [Fact]
    public void AddApplicationServices_ResolvesLookupService()
    {
        var services = new ServiceCollection();
        var gatewayMock = new Mock<IStoredProcedureGateway>();
        services.AddSingleton<IStoredProcedureGateway>(gatewayMock.Object);

        services.AddApplicationServices();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var lookupService = scope.ServiceProvider.GetRequiredService<ILookupService>();

        Assert.NotNull(lookupService);
    }
}
