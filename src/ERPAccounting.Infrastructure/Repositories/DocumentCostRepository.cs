using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ERPAccounting.Infrastructure.Repositories;

public class DocumentCostRepository : IDocumentCostRepository
{
    private readonly AppDbContext _context;

    public DocumentCostRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DocumentCost>> GetByDocumentAsync(int documentId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentCosts
            .Include(cost => cost.CostLineItems)
                .ThenInclude(item => item.VATItems)
            .AsNoTracking()
            .Where(cost => cost.IDDokument == documentId)
            .OrderBy(cost => cost.IDDokumentTroskovi)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentCost?> GetAsync(int documentId, int costId, bool track = false, CancellationToken cancellationToken = default)
    {
        if (track)
        {
            return await _context.DocumentCosts
                .AsTracking()
                .Where(cost => cost.IDDokumentTroskovi == costId && cost.IDDokument == documentId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        IQueryable<DocumentCost> query = _context.DocumentCosts;

        // Avoid loading child collections on tracked queries to prevent marking the full graph as modified
        // when only the header needs updating.
        if (includeChildren && !track)
        {
            query = query
                .Include(cost => cost.CostLineItems)
                    .ThenInclude(item => item.VATItems)
                .AsNoTracking();
        }

        return await query
            .AsNoTracking()
            .Where(cost => cost.IDDokumentTroskovi == costId && cost.IDDokument == documentId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DocumentCost?> GetDetailedAsync(
        int documentId,
        int costId,
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentCosts
            .Include(cost => cost.CostLineItems)
                .ThenInclude(item => item.VATItems)
            .AsSplitQuery()
            .AsQueryable();

        if (track)
        {
            return await query
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await query
            .Include(cost => cost.CostLineItems)
                .ThenInclude(item => item.VATItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(DocumentCost entity, CancellationToken cancellationToken = default)
    {
        await _context.DocumentCosts.AddAsync(entity, cancellationToken);
    }

    public void Update(DocumentCost entity)
    {
        _context.DocumentCosts.Attach(entity);

        // Ensure only the header is marked as modified; keep loaded cost lines/VAT items untouched.
        _context.Entry(entity).State = EntityState.Modified;
        foreach (var lineItem in entity.CostLineItems)
        {
            var lineEntry = _context.Entry(lineItem);
            lineEntry.State = EntityState.Unchanged;

            foreach (var vatItem in lineItem.VATItems)
            {
                _context.Entry(vatItem).State = EntityState.Unchanged;
            }
        }
    }

    public void Remove(DocumentCost entity)
    {
        _context.DocumentCosts.Remove(entity);
    }
}
