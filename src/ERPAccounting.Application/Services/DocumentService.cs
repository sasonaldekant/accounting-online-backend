using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.DTOs.Documents;

namespace ERPAccounting.Application.Services;

/// <summary>
/// Privremena implementacija dok se ne doda prava logika
/// </summary>
public class DocumentService : IDocumentService
{
    public Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto dto)
        => throw new NotImplementedException("DocumentService.CreateDocumentAsync nije još implementiran");

    public Task<bool> DeleteDocumentAsync(int documentId)
        => throw new NotImplementedException("DocumentService.DeleteDocumentAsync nije još implementiran");

    public Task<DocumentDto?> GetDocumentByIdAsync(int documentId)
        => throw new NotImplementedException("DocumentService.GetDocumentByIdAsync nije još implementiran");

    public Task<PaginatedResult<DocumentDto>> GetDocumentsAsync(DocumentQueryParameters query)
        => throw new NotImplementedException("DocumentService.GetDocumentsAsync nije još implementiran");

    public Task<DocumentDto> UpdateDocumentAsync(int documentId, byte[] expectedRowVersion, UpdateDocumentDto dto)
        => throw new NotImplementedException("DocumentService.UpdateDocumentAsync nije još implementiran");
}
