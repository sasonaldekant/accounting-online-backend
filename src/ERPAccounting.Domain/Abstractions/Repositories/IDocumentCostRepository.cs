using ERPAccounting.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ERPAccounting.Domain.Abstractions.Repositories;

/// <summary>
/// Repository abstraction for dependent cost headers.
/// </summary>
public interface IDocumentCostRepository
{
    Task<IReadOnlyList<DocumentCost>> GetByDocumentAsync(int documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentCost>> GetDetailedByDocumentAsync(int documentId, CancellationToken cancellationToken = default);

    Task<DocumentCost?> GetAsync(int documentId, int costId, bool track = false, CancellationToken cancellationToken = default);

    Task<DocumentCost?> GetDetailedAsync(int documentId, int costId, bool track = false, CancellationToken cancellationToken = default);

    Task AddAsync(DocumentCost entity, CancellationToken cancellationToken = default);

    void Update(DocumentCost entity);

    void Remove(DocumentCost entity);
}
