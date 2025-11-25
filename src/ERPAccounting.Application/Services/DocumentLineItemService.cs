using AutoMapper;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Common.Constants;
using ERPAccounting.Common.Exceptions;
using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ERPAccounting.Application.Services
{
    /// <summary>
    /// Implementacija servisa za stavke dokumenta koja upravlja validacijom, mapiranjem i konkurentnošću.
    /// </summary>
    public class DocumentLineItemService : IDocumentLineItemService
    {
        private readonly IDocumentLineItemRepository _lineItemRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<CreateLineItemDto> _createValidator;
        private readonly IValidator<PatchLineItemDto> _patchValidator;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentLineItemService> _logger;

        public DocumentLineItemService(
            IDocumentLineItemRepository lineItemRepository,
            IDocumentRepository documentRepository,
            IUnitOfWork unitOfWork,
            IValidator<CreateLineItemDto> createValidator,
            IValidator<PatchLineItemDto> patchValidator,
            IMapper mapper,
            ILogger<DocumentLineItemService> logger)
        {
            _lineItemRepository = lineItemRepository;
            _documentRepository = documentRepository;
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _patchValidator = patchValidator;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IReadOnlyList<DocumentLineItemDto>> GetItemsAsync(int documentId)
        {
            var items = await _lineItemRepository.GetByDocumentAsync(documentId);

            return _mapper.Map<List<DocumentLineItemDto>>(items);
        }

        public async Task<DocumentLineItemDto?> GetAsync(int documentId, int itemId)
        {
            var entity = await _lineItemRepository.GetAsync(documentId, itemId);

            return entity is null ? null : _mapper.Map<DocumentLineItemDto>(entity);
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
                Opis = dto.Description
            };

            await _lineItemRepository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DocumentLineItemDto>(entity);
        }

        public async Task<DocumentLineItemDto> UpdateAsync(int documentId, int itemId, byte[] expectedRowVersion, PatchLineItemDto dto)
        {
            await ValidateAsync(_patchValidator, dto);

            var entity = await _lineItemRepository.GetAsync(documentId, itemId, track: true);

            if (entity is null)
            {
                throw new NotFoundException(ErrorMessages.DocumentLineItemNotFound, itemId.ToString(), nameof(DocumentLineItem));
            }

            if (entity.StavkaDokumentaTimeStamp is null || !entity.StavkaDokumentaTimeStamp.SequenceEqual(expectedRowVersion))
            {
                _logger.LogWarning("RowVersion mismatch for item {ItemId}", itemId);
                var currentEtag = entity.StavkaDokumentaTimeStamp is null
                    ? string.Empty
                    : Convert.ToBase64String(entity.StavkaDokumentaTimeStamp);
                var expectedEtag = Convert.ToBase64String(expectedRowVersion);
                throw new ConflictException(
                    ErrorMessages.ConcurrencyConflict,
                    itemId.ToString(),
                    nameof(DocumentLineItem),
                    expectedEtag,
                    currentEtag);
            }

            ApplyPatch(entity, dto);
            _lineItemRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DocumentLineItemDto>(entity);
        }

        public async Task DeleteAsync(int documentId, int itemId)
        {
            var entity = await _lineItemRepository.GetAsync(documentId, itemId, track: true);

            if (entity is null)
            {
                throw new NotFoundException(ErrorMessages.DocumentLineItemNotFound, itemId.ToString(), nameof(DocumentLineItem));
            }

            // NOTE: Changed from soft delete to hard delete
            // Database schema does not have IsDeleted column
            // Use ApiAuditLog tables for audit trail instead
            _lineItemRepository.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
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
            var exists = await _documentRepository.ExistsAsync(documentId);

            if (!exists)
            {
                throw new NotFoundException(ErrorMessages.DocumentNotFound, documentId.ToString(), nameof(Document));
            }
        }

        private static async Task ValidateAsync<T>(IValidator<T> validator, T instance)
        {
            var validationResult = await validator.ValidateAsync(instance);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(failure => failure.PropertyName ?? string.Empty)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(failure => failure.ErrorMessage).ToArray());

                throw new Common.Exceptions.ValidationException(ErrorMessages.ValidationFailed, errors);
            }
        }

    }
}
