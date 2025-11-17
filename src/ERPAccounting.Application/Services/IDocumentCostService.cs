using System.Collections.Generic;
using ERPAccounting.Application.DTOs.Costs;

namespace ERPAccounting.Application.Services;

/// <summary>
/// Interfejs za rad sa zavisnim troškovima i njihovim stavkama
/// </summary>
public interface IDocumentCostService
{
    Task<IReadOnlyList<DocumentCostDto>> GetCostsAsync(int documentId);
    Task<DocumentCostDto?> GetCostByIdAsync(int documentId, int costId);
    Task<DocumentCostDto> CreateCostAsync(int documentId, CreateDocumentCostDto dto);
    Task<DocumentCostDto> UpdateCostAsync(int documentId, int costId, byte[] expectedRowVersion, UpdateDocumentCostDto dto);
    Task<bool> DeleteCostAsync(int documentId, int costId);

    Task<IReadOnlyList<DocumentCostItemDto>> GetCostItemsAsync(int documentId, int costId);
    Task<DocumentCostItemDto?> GetCostItemByIdAsync(int documentId, int costId, int itemId);
    Task<DocumentCostItemDto> CreateCostItemAsync(int documentId, int costId, CreateDocumentCostItemDto dto);
    Task<DocumentCostItemDto> UpdateCostItemAsync(int documentId, int costId, int itemId, byte[] expectedRowVersion, PatchDocumentCostItemDto dto);
    Task<bool> DeleteCostItemAsync(int documentId, int costId, int itemId);

    Task<CostDistributionResultDto> DistributeCostAsync(int documentId, int costId, CostDistributionRequestDto dto);
}
