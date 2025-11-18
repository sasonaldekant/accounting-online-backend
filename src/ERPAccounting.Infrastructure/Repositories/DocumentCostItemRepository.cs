using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ERPAccounting.Infrastructure.Repositories;

public class DocumentCostItemRepository : IDocumentCostItemRepository
{
    private readonly AppDbContext _context;

    public DocumentCostItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DocumentCostLineItem>> GetByCostAsync(int costId, bool track = false, CancellationToken cancellationToken = default)
    {
        IQueryable<DocumentCostLineItem> query = _context.DocumentCostLineItems
            .Where(item => item.IDDokumentTroskovi == costId && !item.IsDeleted)
            .OrderBy(item => item.IDDokumentTroskoviStavka);

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<DocumentCostLineItem?> GetAsync(int costId, int itemId, bool track = false, CancellationToken cancellationToken = default)
    {
        IQueryable<DocumentCostLineItem> query = _context.DocumentCostLineItems
            .Where(item => item.IDDokumentTroskoviStavka == itemId && item.IDDokumentTroskovi == costId && !item.IsDeleted);

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(DocumentCostLineItem entity, CancellationToken cancellationToken = default)
    {
        await _context.DocumentCostLineItems.AddAsync(entity, cancellationToken);
    }

    public void Update(DocumentCostLineItem entity)
    {
        _context.DocumentCostLineItems.Update(entity);
    }

    public void UpdateRange(IEnumerable<DocumentCostLineItem> entities)
    {
        _context.DocumentCostLineItems.UpdateRange(entities);
    }
}
