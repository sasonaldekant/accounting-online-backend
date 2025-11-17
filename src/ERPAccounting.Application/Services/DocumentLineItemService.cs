using ERPAccounting.Application.DTOs;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ERPAccounting.Application.Services
{
    /// <summary>
    /// Implementacija servisa za stavke dokumenta koja upravlja validacijom, mapiranjem i konkurentnošću.
    /// </summary>
    public class DocumentLineItemService : IDocumentLineItemService
    {
        private readonly AppDbContext _context;
        private readonly IValidator<CreateLineItemDto> _createValidator;
        private readonly IValidator<PatchLineItemDto> _patchValidator;
        private readonly ILogger<DocumentLineItemService> _logger;

        public DocumentLineItemService(
            AppDbContext context,
            IValidator<CreateLineItemDto> createValidator,
            IValidator<PatchLineItemDto> patchValidator,
            ILogger<DocumentLineItemService> logger)
        {
            _context = context;
            _createValidator = createValidator;
            _patchValidator = patchValidator;
            _logger = logger;
        }

        public async Task<IReadOnlyList<DocumentLineItemDto>> GetItemsAsync(int documentId)
        {
            var items = await _context.DocumentLineItems
                .AsNoTracking()
                .Where(item => item.IDDokument == documentId && !item.IsDeleted)
                .OrderBy(item => item.IDStavkaDokumenta)
                .ToListAsync();

            return items.Select(MapToDto).ToList();
        }

        public async Task<DocumentLineItemDto?> GetAsync(int documentId, int itemId)
        {
            var entity = await _context.DocumentLineItems
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.IDStavkaDokumenta == itemId &&
                    item.IDDokument == documentId &&
                    !item.IsDeleted);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<DocumentLineItemDto> CreateAsync(int documentId, CreateLineItemDto dto)
        {
            await ValidateAsync(_createValidator, dto);
            await EnsureDocumentExistsAsync(documentId);

            var entity = new DocumentLineItem
            {
                IDDokument = documentId,
                IDArtikal = dto.ArticleId,
                IDOrganizacionaJedinica = dto.OrganizationalUnitId,
                Kolicina = dto.Quantity,
                FakturnaCena = dto.InvoicePrice,
                RabatDokument = dto.DiscountAmount ?? 0,
                Marza = dto.MarginAmount ?? 0,
                IDPoreskaStopa = dto.TaxRateId,
                ObracunAkciza = (short)(dto.CalculateExcise ? 1 : 0),
                ObracunPorez = (short)(dto.CalculateTax ? 1 : 0),
                Opis = dto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.DocumentLineItems.Add(entity);
            await _context.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<DocumentLineItemDto> UpdateAsync(int documentId, int itemId, byte[] expectedRowVersion, PatchLineItemDto dto)
        {
            await ValidateAsync(_patchValidator, dto);

            var entity = await _context.DocumentLineItems
                .FirstOrDefaultAsync(item =>
                    item.IDStavkaDokumenta == itemId &&
                    item.IDDokument == documentId &&
                    !item.IsDeleted);

            if (entity is null)
            {
                throw new KeyNotFoundException("Stavka nije pronađena");
            }

            if (entity.StavkaDokumentaTimeStamp is null || !entity.StavkaDokumentaTimeStamp.SequenceEqual(expectedRowVersion))
            {
                _logger.LogWarning("RowVersion mismatch for item {ItemId}", itemId);
                throw new DbUpdateConcurrencyException("RowVersion mismatch");
            }

            ApplyPatch(entity, dto);
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<bool> DeleteAsync(int documentId, int itemId)
        {
            var entity = await _context.DocumentLineItems
                .FirstOrDefaultAsync(item =>
                    item.IDStavkaDokumenta == itemId &&
                    item.IDDokument == documentId &&
                    !item.IsDeleted);

            if (entity is null)
            {
                return false;
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static void ApplyPatch(DocumentLineItem entity, PatchLineItemDto dto)
        {
            if (dto.Quantity.HasValue)
            {
                entity.Kolicina = dto.Quantity.Value;
            }

            if (dto.InvoicePrice.HasValue)
            {
                entity.FakturnaCena = dto.InvoicePrice.Value;
            }

            if (dto.DiscountAmount.HasValue)
            {
                entity.RabatDokument = dto.DiscountAmount.Value;
            }

            if (dto.MarginAmount.HasValue)
            {
                entity.Marza = dto.MarginAmount.Value;
            }

            if (dto.TaxRateId is not null)
            {
                entity.IDPoreskaStopa = dto.TaxRateId;
            }

            if (dto.CalculateExcise.HasValue)
            {
                entity.ObracunAkciza = (short)(dto.CalculateExcise.Value ? 1 : 0);
            }

            if (dto.CalculateTax.HasValue)
            {
                entity.ObracunPorez = (short)(dto.CalculateTax.Value ? 1 : 0);
            }

            if (dto.Description is not null)
            {
                entity.Opis = dto.Description;
            }
        }

        private async Task EnsureDocumentExistsAsync(int documentId)
        {
            var exists = await _context.Documents
                .AsNoTracking()
                .AnyAsync(document => document.IDDokument == documentId);

            if (!exists)
            {
                throw new KeyNotFoundException("Dokument nije pronađen");
            }
        }

        private static async Task ValidateAsync<T>(IValidator<T> validator, T instance)
        {
            var validationResult = await validator.ValidateAsync(instance);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        private static DocumentLineItemDto MapToDto(DocumentLineItem entity)
        {
            var etag = entity.StavkaDokumentaTimeStamp is null
                ? string.Empty
                : Convert.ToBase64String(entity.StavkaDokumentaTimeStamp);

            return new DocumentLineItemDto(
                Id: entity.IDStavkaDokumenta,
                DocumentId: entity.IDDokument,
                ArticleId: entity.IDArtikal,
                Quantity: entity.Kolicina,
                InvoicePrice: entity.FakturnaCena,
                DiscountAmount: entity.RabatDokument,
                MarginAmount: entity.Marza,
                TaxRateId: entity.IDPoreskaStopa,
                TaxPercent: entity.ProcenatPoreza,
                TaxAmount: entity.IznosPDV,
                Total: entity.Iznos,
                CalculateExcise: entity.ObracunAkciza == 1,
                CalculateTax: entity.ObracunPorez == 1,
                Description: entity.Opis,
                ETag: etag,
                CreatedAt: entity.CreatedAt,
                UpdatedAt: entity.UpdatedAt,
                CreatedBy: entity.CreatedBy,
                UpdatedBy: entity.UpdatedBy
            );
        }
    }
}
