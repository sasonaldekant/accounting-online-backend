using ERPAccounting.Application.DTOs.Costs;

namespace ERPAccounting.Application.Services;

/// <summary>
/// Privremena implementacija servisa za zavisne troškove
/// </summary>
public class DocumentCostService : IDocumentCostService
{
    public Task<DocumentCostItemDto> CreateCostItemAsync(int documentId, int costId, CreateDocumentCostItemDto dto)
        => throw new NotImplementedException("DocumentCostService.CreateCostItemAsync nije još implementiran");

    public Task<DocumentCostDto> CreateCostAsync(int documentId, CreateDocumentCostDto dto)
        => throw new NotImplementedException("DocumentCostService.CreateCostAsync nije još implementiran");

    public Task<bool> DeleteCostAsync(int documentId, int costId)
        => throw new NotImplementedException("DocumentCostService.DeleteCostAsync nije još implementiran");

    public Task<bool> DeleteCostItemAsync(int documentId, int costId, int itemId)
        => throw new NotImplementedException("DocumentCostService.DeleteCostItemAsync nije još implementiran");

    public Task<CostDistributionResultDto> DistributeCostAsync(int documentId, int costId, CostDistributionRequestDto dto)
        => throw new NotImplementedException("DocumentCostService.DistributeCostAsync nije još implementiran");

    public Task<DocumentCostDto?> GetCostByIdAsync(int documentId, int costId)
        => throw new NotImplementedException("DocumentCostService.GetCostByIdAsync nije još implementiran");

    public Task<IReadOnlyList<DocumentCostDto>> GetCostsAsync(int documentId)
        => throw new NotImplementedException("DocumentCostService.GetCostsAsync nije još implementiran");

    public Task<DocumentCostItemDto?> GetCostItemByIdAsync(int documentId, int costId, int itemId)
        => throw new NotImplementedException("DocumentCostService.GetCostItemByIdAsync nije još implementiran");

    public Task<IReadOnlyList<DocumentCostItemDto>> GetCostItemsAsync(int documentId, int costId)
        => throw new NotImplementedException("DocumentCostService.GetCostItemsAsync nije još implementiran");

    public Task<DocumentCostItemDto> UpdateCostItemAsync(int documentId, int costId, int itemId, byte[] expectedRowVersion, PatchDocumentCostItemDto dto)
        => throw new NotImplementedException("DocumentCostService.UpdateCostItemAsync nije još implementiran");

    public Task<DocumentCostDto> UpdateCostAsync(int documentId, int costId, byte[] expectedRowVersion, UpdateDocumentCostDto dto)
        => throw new NotImplementedException("DocumentCostService.UpdateCostAsync nije još implementiran");
}
