using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Infrastructure.Data;

namespace ERPAccounting.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(
        AppDbContext context,
        IDocumentLineItemRepository documentLineItemRepository,
        IDocumentRepository documentRepository)
    {
        _context = context;
        DocumentLineItems = documentLineItemRepository;
        Documents = documentRepository;
    }

    public IDocumentLineItemRepository DocumentLineItems { get; }

    public IDocumentRepository Documents { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
