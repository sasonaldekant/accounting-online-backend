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
        return await GetDetailedByDocumentAsync(documentId, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentCost>> GetDetailedByDocumentAsync(int documentId, CancellationToken cancellationToken = default)
    {
        return await BuildDetailedQuery(track: false)
            .Where(cost => cost.IDDokument == documentId)
            .OrderBy(cost => cost.IDDokumentTroskovi)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentCost?> GetAsync(int documentId, int costId, bool track = false, CancellationToken cancellationToken = default)
    {
        return await GetDetailedAsync(documentId, costId, track, cancellationToken);
    }

    public async Task<DocumentCost?> GetDetailedAsync(int documentId, int costId, bool track = false, CancellationToken cancellationToken = default)
    {
        return await BuildDetailedQuery(track)
            .Where(cost => cost.IDDokumentTroskovi == costId && cost.IDDokument == documentId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<DocumentCost> BuildDetailedQuery(bool track)
    {
        var query = _context.DocumentCosts
            .Include(cost => cost.CostLineItems)
                .ThenInclude(item => item.VATItems)
            .AsSplitQuery()
            .AsQueryable();

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return query;
    }

    public async Task AddAsync(DocumentCost entity, CancellationToken cancellationToken = default)
    {
        await _context.DocumentCosts.AddAsync(entity, cancellationToken);
    }

    public void Update(DocumentCost entity)
    {
        _context.DocumentCosts.Update(entity);
    }

    public void Remove(DocumentCost entity)
    {
        _context.DocumentCosts.Remove(entity);
    }
}
