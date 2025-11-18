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
            .AsNoTracking()
            .Where(cost => cost.IDDokument == documentId && !cost.IsDeleted)
            .OrderBy(cost => cost.IDDokumentTroskovi)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentCost?> GetAsync(int documentId, int costId, bool track = false, CancellationToken cancellationToken = default)
    {
        IQueryable<DocumentCost> query = _context.DocumentCosts
            .Where(cost => cost.IDDokumentTroskovi == costId && cost.IDDokument == documentId && !cost.IsDeleted);

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(DocumentCost entity, CancellationToken cancellationToken = default)
    {
        await _context.DocumentCosts.AddAsync(entity, cancellationToken);
    }

    public void Update(DocumentCost entity)
    {
        _context.DocumentCosts.Update(entity);
    }
}
