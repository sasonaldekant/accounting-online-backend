using ERPAccounting.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ERPAccounting.Domain.Abstractions.Repositories;

/// <summary>
/// Repository abstraction for accessing and modifying document line items without exposing EF Core details.
/// </summary>
public interface IDocumentLineItemRepository
{
    Task<IReadOnlyList<DocumentLineItem>> GetByDocumentAsync(int documentId, CancellationToken cancellationToken = default);

    Task<DocumentLineItem?> GetAsync(int documentId, int itemId, bool track = false, CancellationToken cancellationToken = default);

    Task AddAsync(DocumentLineItem entity, CancellationToken cancellationToken = default);

    void Update(DocumentLineItem entity);
}
