using ERPAccounting.Application.DTOs;

namespace ERPAccounting.Application.Services
{
    /// <summary>
    /// Poslovna logika za rad sa stavkama dokumenta.
    /// Sloj servisa brine o validaciji, mapiranju i konkurentnosti.
    /// </summary>
    public interface IDocumentLineItemService
    {
        Task<IReadOnlyList<DocumentLineItemDto>> GetItemsAsync(int documentId);
        Task<DocumentLineItemDto?> GetAsync(int documentId, int itemId);
        Task<DocumentLineItemDto> CreateAsync(int documentId, CreateLineItemDto dto);
        Task<DocumentLineItemDto> UpdateAsync(int documentId, int itemId, byte[] expectedRowVersion, PatchLineItemDto dto);
        Task<bool> DeleteAsync(int documentId, int itemId);
    }
}
