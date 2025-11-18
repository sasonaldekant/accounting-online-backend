using System.Threading;
using System.Threading.Tasks;

namespace ERPAccounting.Domain.Abstractions.Repositories;

/// <summary>
/// Repository abstraction for high-level document data access used by application services.
/// </summary>
public interface IDocumentRepository
{
    Task<bool> ExistsAsync(int documentId, CancellationToken cancellationToken = default);
}
