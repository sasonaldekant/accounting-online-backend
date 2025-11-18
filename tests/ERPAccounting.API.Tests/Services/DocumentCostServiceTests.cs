using ERPAccounting.Application.DTOs.Costs;
using ERPAccounting.Application.Services;
using ERPAccounting.Application.Validators;
using ERPAccounting.Common.Exceptions;
using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPAccounting.API.Tests.Services;

public class DocumentCostServiceTests
{
    [Fact]
    public async Task CreateCostAsync_SavesAndReturnsDto()
    {
        var costRepo = new Mock<IDocumentCostRepository>();
        var costItemRepo = new Mock<IDocumentCostItemRepository>();
        var documentRepo = new Mock<IDocumentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        documentRepo.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        costRepo.Setup(r => r.AddAsync(It.IsAny<DocumentCost>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentCost, CancellationToken>((entity, _) =>
            {
                entity.IDDokumentTroskovi = 7;
                entity.DokumentTroskoviTimeStamp = new byte[] { 1, 2, 3, 4 };
            })
            .Returns(Task.CompletedTask);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService(costRepo, costItemRepo, documentRepo, unitOfWork);

        var dto = new CreateDocumentCostDto(3, 150, 30, DateTime.UtcNow, "Trošak");
        var result = await service.CreateCostAsync(1, dto);

        Assert.Equal(7, result.Id);
        Assert.Equal(150, result.AmountNet);
        Assert.Equal(30, result.AmountVat);
        Assert.Equal("AQIDBA==", result.ETag);
        costRepo.Verify(r => r.AddAsync(It.IsAny<DocumentCost>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCostAsync_RowVersionMismatch_ThrowsConflict()
    {
        var costRepo = new Mock<IDocumentCostRepository>();
        var costItemRepo = new Mock<IDocumentCostItemRepository>();
        var documentRepo = new Mock<IDocumentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var entity = new DocumentCost
        {
            IDDokument = 1,
            IDDokumentTroskovi = 2,
            DokumentTroskoviTimeStamp = new byte[] { 1 }
        };

        costRepo.Setup(r => r.GetAsync(1, 2, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService(costRepo, costItemRepo, documentRepo, unitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateCostAsync(1, 2, new byte[] { 9 }, new UpdateDocumentCostDto(3, 100, 10, DateTime.UtcNow, null)));
    }

    [Fact]
    public async Task UpdateCostItemAsync_RowVersionMismatch_ThrowsConflict()
    {
        var costRepo = new Mock<IDocumentCostRepository>();
        var costItemRepo = new Mock<IDocumentCostItemRepository>();
        var documentRepo = new Mock<IDocumentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        costRepo.Setup(r => r.GetAsync(1, 2, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentCost { IDDokument = 1, IDDokumentTroskovi = 2 });

        var item = new DocumentCostLineItem
        {
            IDDokumentTroskovi = 2,
            IDDokumentTroskoviStavka = 3,
            DokumentTroskoviStavkaTimeStamp = new byte[] { 1 }
        };

        costItemRepo.Setup(r => r.GetAsync(2, 3, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = CreateService(costRepo, costItemRepo, documentRepo, unitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateCostItemAsync(1, 2, 3, new byte[] { 5 }, new PatchDocumentCostItemDto(null, null, null, null, null)));
    }

    [Fact]
    public async Task DistributeCostAsync_ManualWithoutPayload_ThrowsValidation()
    {
        var costRepo = new Mock<IDocumentCostRepository>();
        var costItemRepo = new Mock<IDocumentCostItemRepository>();
        var documentRepo = new Mock<IDocumentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        costRepo.Setup(r => r.GetAsync(1, 2, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentCost { IDDokument = 1, IDDokumentTroskovi = 2 });

        costItemRepo.Setup(r => r.GetByCostAsync(2, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentCostLineItem> { new() { IDDokumentTroskovi = 2, IDDokumentTroskoviStavka = 5 } });

        var service = CreateService(costRepo, costItemRepo, documentRepo, unitOfWork);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.DistributeCostAsync(1, 2, new CostDistributionRequestDto(3, null)));
    }

    [Fact]
    public async Task DistributeCostAsync_ByQuantity_DistributesAmount()
    {
        var costRepo = new Mock<IDocumentCostRepository>();
        var costItemRepo = new Mock<IDocumentCostItemRepository>();
        var documentRepo = new Mock<IDocumentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        costRepo.Setup(r => r.GetAsync(1, 2, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentCost { IDDokument = 1, IDDokumentTroskovi = 2, IznosBezPDV = 120 });

        var items = new List<DocumentCostLineItem>
        {
            new() { IDDokumentTroskovi = 2, IDDokumentTroskoviStavka = 1, Kolicina = 1 },
            new() { IDDokumentTroskovi = 2, IDDokumentTroskoviStavka = 2, Kolicina = 2 }
        };

        costItemRepo.Setup(r => r.GetByCostAsync(2, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService(costRepo, costItemRepo, documentRepo, unitOfWork);

        var result = await service.DistributeCostAsync(1, 2, new CostDistributionRequestDto(1, null));

        Assert.Equal(2, result.ProcessedItems);
        Assert.Equal(120, result.DistributedAmount);
        Assert.Equal(40, items[0].Iznos);
        Assert.Equal(80, items[1].Iznos);
        costItemRepo.Verify(r => r.UpdateRange(It.IsAny<IEnumerable<DocumentCostLineItem>>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DocumentCostService CreateService(
        Mock<IDocumentCostRepository> costRepo,
        Mock<IDocumentCostItemRepository> costItemRepo,
        Mock<IDocumentRepository> documentRepo,
        Mock<IUnitOfWork> unitOfWork)
        => new(
            costRepo.Object,
            costItemRepo.Object,
            documentRepo.Object,
            unitOfWork.Object,
            new CreateDocumentCostValidator(),
            new UpdateDocumentCostValidator(),
            new CreateDocumentCostItemValidator(),
            new PatchDocumentCostItemValidator(),
            NullLogger<DocumentCostService>.Instance);
}
