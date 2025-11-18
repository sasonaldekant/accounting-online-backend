using System.Threading;
using System.Threading.Tasks;

namespace ERPAccounting.Domain.Abstractions.Repositories;

/// <summary>
/// Defines a unit-of-work contract that coordinates repository operations within a single persistence context.
/// </summary>
public interface IUnitOfWork
{
    IDocumentLineItemRepository DocumentLineItems { get; }

    IDocumentRepository Documents { get; }

    IDocumentCostRepository DocumentCosts { get; }

    IDocumentCostItemRepository DocumentCostItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
