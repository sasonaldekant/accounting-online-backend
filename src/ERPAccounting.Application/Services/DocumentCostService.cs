using ERPAccounting.Application.DTOs.Costs;
using ERPAccounting.Common.Constants;
using ERPAccounting.Common.Exceptions;
using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ERPAccounting.Application.Services;

/// <summary>
/// Implementacija servisa za upravljanje zavisnim troškovima i njihovim stavkama.
/// </summary>
public class DocumentCostService : IDocumentCostService
{
    private readonly IDocumentCostRepository _costRepository;
    private readonly IDocumentCostItemRepository _costItemRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateDocumentCostDto> _createCostValidator;
    private readonly IValidator<UpdateDocumentCostDto> _updateCostValidator;
    private readonly IValidator<CreateDocumentCostItemDto> _createCostItemValidator;
    private readonly IValidator<PatchDocumentCostItemDto> _patchCostItemValidator;
    private readonly ILogger<DocumentCostService> _logger;

    public DocumentCostService(
        IDocumentCostRepository costRepository,
        IDocumentCostItemRepository costItemRepository,
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateDocumentCostDto> createCostValidator,
        IValidator<UpdateDocumentCostDto> updateCostValidator,
        IValidator<CreateDocumentCostItemDto> createCostItemValidator,
        IValidator<PatchDocumentCostItemDto> patchCostItemValidator,
        ILogger<DocumentCostService> logger)
    {
        _costRepository = costRepository;
        _costItemRepository = costItemRepository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _createCostValidator = createCostValidator;
        _updateCostValidator = updateCostValidator;
        _createCostItemValidator = createCostItemValidator;
        _patchCostItemValidator = patchCostItemValidator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentCostDto>> GetCostsAsync(int documentId)
    {
        var costs = await _costRepository.GetByDocumentAsync(documentId);
        return costs.Select(cost => MapToDto(cost)).ToList();
    }

    public async Task<DocumentCostDto?> GetCostByIdAsync(int documentId, int costId)
    {
        var entity = await _costRepository.GetAsync(documentId, costId);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<DocumentCostDto> CreateCostAsync(int documentId, CreateDocumentCostDto dto)
    {
        await ValidateAsync(_createCostValidator, dto);
        await EnsureDocumentExistsAsync(documentId);
        var documentTypeCode = dto.DocumentTypeCode.Trim();

        var entity = new DocumentCost
        {
            IDDokument = documentId,
            IDPartner = dto.PartnerId,
            IDVrstaDokumenta = documentTypeCode,
            IznosBezPDV = dto.AmountNet,
            IznosPDV = dto.AmountVat,
            DatumValute = dto.DueDate,
            Opis = dto.Description,
            DatumDPO = DateTime.UtcNow,
            BrojDokumenta = $"{documentTypeCode}-{documentId}-{DateTime.UtcNow.Ticks}",
            IDStatus = 1,
            NazivTroska = null
        };

        await _costRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<DocumentCostDto> UpdateCostAsync(int documentId, int costId, byte[] expectedRowVersion, UpdateDocumentCostDto dto)
    {
        await ValidateAsync(_updateCostValidator, dto);

        var entity = await EnsureCostExistsAsync(documentId, costId, track: true);

        EnsureRowVersion(entity.DokumentTroskoviTimeStamp, expectedRowVersion, costId, nameof(DocumentCost));
        var documentTypeCode = dto.DocumentTypeCode.Trim();

        entity.IDPartner = dto.PartnerId;
        entity.IDVrstaDokumenta = documentTypeCode;
        entity.IznosBezPDV = dto.AmountNet;
        entity.IznosPDV = dto.AmountVat;
        entity.DatumValute = dto.DueDate;
        entity.Opis = dto.Description;

        _costRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<bool> DeleteCostAsync(int documentId, int costId)
    {
        var entity = await _costRepository.GetAsync(documentId, costId, track: true);
        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<DocumentCostItemDto>> GetCostItemsAsync(int documentId, int costId)
    {
        await EnsureCostExistsAsync(documentId, costId);
        var items = await _costItemRepository.GetByCostAsync(costId);
        return items.Select(item => MapToItemDto(item)).ToList();
    }

    public async Task<DocumentCostItemDto?> GetCostItemByIdAsync(int documentId, int costId, int itemId)
    {
        await EnsureCostExistsAsync(documentId, costId);
        var entity = await _costItemRepository.GetAsync(costId, itemId);
        return entity is null ? null : MapToItemDto(entity);
    }

    public async Task<DocumentCostItemDto> CreateCostItemAsync(int documentId, int costId, CreateDocumentCostItemDto dto)
    {
        await ValidateAsync(_createCostItemValidator, dto);
        await EnsureCostExistsAsync(documentId, costId);

        var entity = new DocumentCostLineItem
        {
            IDDokumentTroskovi = costId,
            IDUlazniRacuniIzvedeni = dto.ArticleId,
            Kolicina = dto.Quantity,
            Iznos = dto.AmountNet,
            IznosValuta = dto.AmountVat,
            IDNacinDeljenjaTroskova = dto.TaxRateId,
            Napomena = dto.Note,
            SveStavke = false,
            IDStatus = 1
        };

        await _costItemRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return MapToItemDto(entity);
    }

    public async Task<DocumentCostItemDto> UpdateCostItemAsync(int documentId, int costId, int itemId, byte[] expectedRowVersion, PatchDocumentCostItemDto dto)
    {
        await ValidateAsync(_patchCostItemValidator, dto);
        await EnsureCostExistsAsync(documentId, costId);

        var entity = await _costItemRepository.GetAsync(costId, itemId, track: true);
        if (entity is null)
        {
            throw new NotFoundException(ErrorMessages.DocumentCostItemNotFound, itemId.ToString(), nameof(DocumentCostLineItem));
        }

        EnsureRowVersion(entity.DokumentTroskoviStavkaTimeStamp, expectedRowVersion, itemId, nameof(DocumentCostLineItem));

        if (dto.Quantity.HasValue)
        {
            entity.Kolicina = dto.Quantity.Value;
        }

        if (dto.AmountNet.HasValue)
        {
            entity.Iznos = dto.AmountNet.Value;
        }

        if (dto.AmountVat.HasValue)
        {
            entity.IznosValuta = dto.AmountVat.Value;
        }

        if (dto.TaxRateId.HasValue)
        {
            entity.IDNacinDeljenjaTroskova = dto.TaxRateId.Value;
        }

        if (dto.Note is not null)
        {
            entity.Napomena = dto.Note;
        }

        _costItemRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return MapToItemDto(entity);
    }

    public async Task<bool> DeleteCostItemAsync(int documentId, int costId, int itemId)
    {
        await EnsureCostExistsAsync(documentId, costId);
        var entity = await _costItemRepository.GetAsync(costId, itemId, track: true);
        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<CostDistributionResultDto> DistributeCostAsync(int documentId, int costId, CostDistributionRequestDto dto)
    {
        var cost = await EnsureCostExistsAsync(documentId, costId, track: true);
        var items = await _costItemRepository.GetByCostAsync(costId, track: true);

        if (items.Count == 0)
        {
            return new CostDistributionResultDto(costId, 0, 0);
        }

        int processed;
        decimal distributedAmount;

        switch (dto.DistributionMethodId)
        {
            case 1:
                (processed, distributedAmount) = ApplyDistribution(cost.IznosBezPDV, items, item => item.Kolicina ?? 0);
                break;
            case 2:
                (processed, distributedAmount) = ApplyDistribution(cost.IznosBezPDV, items, item => item.Iznos);
                break;
            case 3:
                (processed, distributedAmount) = ApplyManualDistribution(items, dto.ManualDistribution);
                break;
            default:
                throw new Common.Exceptions.ValidationException(ErrorMessages.InvalidCostDistributionMethod, new Dictionary<string, string[]>
                {
                    [nameof(dto.DistributionMethodId)] = new[] { ErrorMessages.InvalidCostDistributionMethod }
                });
        }

        _costItemRepository.UpdateRange(items);
        await _unitOfWork.SaveChangesAsync();

        return new CostDistributionResultDto(costId, processed, distributedAmount);
    }

    private static (int processed, decimal total) ApplyDistribution(decimal totalAmount, IReadOnlyList<DocumentCostLineItem> items, Func<DocumentCostLineItem, decimal> weightSelector)
    {
        if (items.Count == 0)
        {
            return (0, 0);
        }

        var totalWeight = items.Sum(weightSelector);
        if (totalWeight <= 0)
        {
            totalWeight = items.Count;
        }

        var distributed = 0m;
        for (var i = 0; i < items.Count; i++)
        {
            var weight = weightSelector(items[i]);
            if (weight <= 0 && totalWeight == items.Count)
            {
                weight = 1;
            }

            var share = totalWeight == 0
                ? 0
                : Math.Round(totalAmount * (weight / totalWeight), 4, MidpointRounding.AwayFromZero);

            if (i == items.Count - 1)
            {
                share = totalAmount - distributed;
            }

            items[i].Iznos = share;
            distributed += share;
        }

        return (items.Count, distributed);
    }

    private static (int processed, decimal total) ApplyManualDistribution(IReadOnlyList<DocumentCostLineItem> items, IReadOnlyDictionary<int, decimal>? manual)
    {
        if (manual is null || manual.Count == 0)
        {
            throw new Common.Exceptions.ValidationException(ErrorMessages.CostManualDistributionRequired, new Dictionary<string, string[]>
            {
                [nameof(CostDistributionRequestDto.ManualDistribution)] = new[] { ErrorMessages.CostManualDistributionRequired }
            });
        }

        var processed = 0;
        var distributed = 0m;

        foreach (var (itemId, amount) in manual)
        {
            var entity = items.FirstOrDefault(i => i.IDDokumentTroskoviStavka == itemId)
                ?? throw new NotFoundException(ErrorMessages.DocumentCostItemNotFound, itemId.ToString(), nameof(DocumentCostLineItem));

            entity.Iznos = amount;
            processed++;
            distributed += amount;
        }

        return (processed, distributed);
    }

    private async Task EnsureDocumentExistsAsync(int documentId)
    {
        var exists = await _documentRepository.ExistsAsync(documentId);
        if (!exists)
        {
            throw new NotFoundException(ErrorMessages.DocumentNotFound, documentId.ToString(), nameof(Document));
        }
    }

    private async Task<DocumentCost> EnsureCostExistsAsync(int documentId, int costId, bool track = false)
    {
        var entity = await _costRepository.GetAsync(documentId, costId, track);
        if (entity is null)
        {
            throw new NotFoundException(ErrorMessages.DocumentCostNotFound, costId.ToString(), nameof(DocumentCost));
        }

        return entity;
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

    private void EnsureRowVersion(byte[]? current, byte[] expected, int id, string resource)
    {
        if (current is null || !current.SequenceEqual(expected))
        {
            var currentEtag = current is null ? string.Empty : Convert.ToBase64String(current);
            var expectedEtag = Convert.ToBase64String(expected);
            _logger.LogWarning("RowVersion mismatch for {Resource} {Id}", resource, id);
            throw new ConflictException(
                ErrorMessages.ConcurrencyConflict,
                id.ToString(),
                resource,
                expectedEtag,
                currentEtag);
        }
    }

    private static DocumentCostDto MapToDto(DocumentCost entity)
    {
        var etag = entity.DokumentTroskoviTimeStamp is null
            ? string.Empty
            : Convert.ToBase64String(entity.DokumentTroskoviTimeStamp);

        return new DocumentCostDto(
            entity.IDDokumentTroskovi,
            entity.IDDokument,
            entity.IDPartner,
            entity.IDVrstaDokumenta,
            entity.IznosBezPDV,
            entity.IznosPDV,
            entity.DatumValute ?? entity.DatumDPO,
            entity.Opis,
            etag);
    }

    private static DocumentCostItemDto MapToItemDto(DocumentCostLineItem entity)
    {
        var etag = entity.DokumentTroskoviStavkaTimeStamp is null
            ? string.Empty
            : Convert.ToBase64String(entity.DokumentTroskoviStavkaTimeStamp);

        return new DocumentCostItemDto(
            entity.IDDokumentTroskoviStavka,
            entity.IDDokumentTroskovi,
            entity.IDUlazniRacuniIzvedeni,
            entity.Kolicina ?? 0,
            entity.Iznos,
            entity.IznosValuta ?? 0,
            entity.IDNacinDeljenjaTroskova,
            entity.Napomena,
            etag);
    }
}
