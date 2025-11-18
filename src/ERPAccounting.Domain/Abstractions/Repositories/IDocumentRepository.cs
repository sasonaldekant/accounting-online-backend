using ERPAccounting.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ERPAccounting.Domain.Abstractions.Repositories;

/// <summary>
/// Repository abstraction for high-level document data access used by application services.
/// </summary>
public interface IDocumentRepository
{
    Task<bool> ExistsAsync(int documentId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Document> Items, int TotalCount)> GetPaginatedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    Task<Document?> GetByIdAsync(int documentId, bool track = false, CancellationToken cancellationToken = default);

    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    void Update(Document document);
}
