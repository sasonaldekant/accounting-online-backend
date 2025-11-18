using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPAccounting.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(int documentId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .AsNoTracking()
            .AnyAsync(document => document.IDDokument == documentId, cancellationToken);
    }
}
