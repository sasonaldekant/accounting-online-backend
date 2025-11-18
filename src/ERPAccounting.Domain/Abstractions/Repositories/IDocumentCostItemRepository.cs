using ERPAccounting.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ERPAccounting.Domain.Abstractions.Repositories;

/// <summary>
/// Repository abstraction for dependent cost line items.
/// </summary>
public interface IDocumentCostItemRepository
{
    Task<IReadOnlyList<DocumentCostLineItem>> GetByCostAsync(int costId, bool track = false, CancellationToken cancellationToken = default);

    Task<DocumentCostLineItem?> GetAsync(int costId, int itemId, bool track = false, CancellationToken cancellationToken = default);

    Task AddAsync(DocumentCostLineItem entity, CancellationToken cancellationToken = default);

    void Update(DocumentCostLineItem entity);

    void UpdateRange(IEnumerable<DocumentCostLineItem> entities);
}
