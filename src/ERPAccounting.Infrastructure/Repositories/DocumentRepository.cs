using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

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

    public async Task<(IReadOnlyList<Document> Items, int TotalCount)> GetPaginatedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Document> query = _context.Documents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(document => EF.Functions.Like(document.BrojDokumenta, term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(document => document.Datum)
            .ThenBy(document => document.IDDokument)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Document?> GetByIdAsync(int documentId, bool track = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Document> query = _context.Documents
            .Where(document => document.IDDokument == documentId);

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
    }

    public void Update(Document document)
    {
        _context.Documents.Update(document);
    }

    public void Delete(Document document)
    {
        _context.Documents.Remove(document);
    }
}
