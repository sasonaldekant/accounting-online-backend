using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.DTOs.Documents;

namespace ERPAccounting.Application.Services;

/// <summary>
/// Interfejs za rad sa zaglavljima dokumenata
/// </summary>
public interface IDocumentService
{
    Task<PaginatedResult<DocumentDto>> GetDocumentsAsync(DocumentQueryParameters query);
    Task<DocumentDto?> GetDocumentByIdAsync(int documentId);
    Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto dto);
    Task<DocumentDto> UpdateDocumentAsync(int documentId, byte[] expectedRowVersion, UpdateDocumentDto dto);
    Task<bool> DeleteDocumentAsync(int documentId);

    /// <summary>
    /// Pretraga dokumenata sa naprednijim filterima i paginacijom
    /// </summary>
    /// <param name="searchDto">Parametri pretrage</param>
    /// <returns>Paginovani rezultati pretrage</returns>
    Task<PaginatedResult<DocumentDto>> SearchDocumentsAsync(DocumentSearchDto searchDto);
}
