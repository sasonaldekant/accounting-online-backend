using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ERPAccounting.Infrastructure.Repositories;

public class DocumentLineItemRepository : IDocumentLineItemRepository
{
    private readonly AppDbContext _context;

    public DocumentLineItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DocumentLineItem>> GetByDocumentAsync(int documentId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentLineItems
            .AsNoTracking()
            .Where(item => item.IDDokument == documentId && !item.IsDeleted)
            .OrderBy(item => item.IDStavkaDokumenta)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentLineItem?> GetAsync(int documentId, int itemId, bool track = false, CancellationToken cancellationToken = default)
    {
        IQueryable<DocumentLineItem> query = _context.DocumentLineItems
            .Where(item => item.IDStavkaDokumenta == itemId && item.IDDokument == documentId && !item.IsDeleted);

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(DocumentLineItem entity, CancellationToken cancellationToken = default)
    {
        await _context.DocumentLineItems.AddAsync(entity, cancellationToken);
    }

    public void Update(DocumentLineItem entity)
    {
        _context.DocumentLineItems.Update(entity);
    }
}
