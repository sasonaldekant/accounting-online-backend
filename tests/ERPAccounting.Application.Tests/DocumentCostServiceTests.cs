using System.Collections.Generic;
using System.Threading.Tasks;
using ERPAccounting.Application.DTOs.Costs;
using ERPAccounting.Application.Services;
using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERPAccounting.Application.Tests.Services;

public class DocumentCostServiceTests
{
    [Fact]
    public async Task GetCostsAsync_ReturnsMappedDtos()
    {
        // Arrange
        var fakeDocId = 1;
        var entities = new List<DocumentCost>
        {
            new DocumentCost { IDDokumentTroskovi = 10, IDDokument = fakeDocId, IznosBezPDV = 200, IznosPDV = 40 },
            new DocumentCost { IDDokumentTroskovi = 11, IDDokument = fakeDocId, IznosBezPDV = 100, IznosPDV = 20 }
        };
        var repoMock = new Mock<IDocumentCostRepository>();
        repoMock.Setup(r => r.GetByDocumentAsync(fakeDocId)).ReturnsAsync(entities);
        var service = new DocumentCostService(
            repoMock.Object,
            Mock.Of<IDocumentCostItemRepository>(),
            Mock.Of<IDocumentRepository>(),
            Mock.Of<IUnitOfWork>(),
            Mock.Of<FluentValidation.IValidator<CreateDocumentCostDto>>(),
            Mock.Of<FluentValidation.IValidator<UpdateDocumentCostDto>>(),
            Mock.Of<FluentValidation.IValidator<CreateDocumentCostItemDto>>(),
            Mock.Of<FluentValidation.IValidator<PatchDocumentCostItemDto>>(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<DocumentCostService>>()
        );
        // Act
        var result = await service.GetCostsAsync(fakeDocId);
        // Assert
        result.Should().HaveCount(2);
        result[0].IDDokumentTroskovi.Should().Be(10);
        result[1].IznosBezPDV.Should().Be(100);
    }
}
