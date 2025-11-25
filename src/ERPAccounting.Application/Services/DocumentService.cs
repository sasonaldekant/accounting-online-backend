using AutoMapper;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.DTOs.Documents;
using ERPAccounting.Common.Constants;
using ERPAccounting.Common.Exceptions;
using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = ERPAccounting.Common.Exceptions.ValidationException;

namespace ERPAccounting.Application.Services;

/// <summary>
/// Application service responsible for orchestrating document CRUD operations with validation and concurrency handling.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateDocumentDto> _createValidator;
    private readonly IValidator<UpdateDocumentDto> _updateValidator;
    private readonly IValidator<DocumentQueryParameters> _queryValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateDocumentDto> createValidator,
        IValidator<UpdateDocumentDto> updateValidator,
        IValidator<DocumentQueryParameters> queryValidator,
        IMapper mapper,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _queryValidator = queryValidator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResult<DocumentDto>> GetDocumentsAsync(DocumentQueryParameters query)
    {
        query ??= new DocumentQueryParameters();
        await ValidateAsync(_queryValidator, query);

        var (items, totalCount) = await _documentRepository.GetPaginatedAsync(query.Page, query.PageSize, query.Search);
        var dtoItems = _mapper.Map<List<DocumentDto>>(items);

        return new PaginatedResult<DocumentDto>(dtoItems, totalCount, query.Page, query.PageSize);
    }

    public async Task<DocumentDto?> GetDocumentByIdAsync(int documentId)
    {
        var entity = await _documentRepository.GetByIdAsync(documentId);
        return entity is null ? null : _mapper.Map<DocumentDto>(entity);
    }

    public async Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await ValidateAsync(_createValidator, dto);

        var entity = _mapper.Map<Document>(dto);

        await _documentRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<DocumentDto>(entity);
    }

    public async Task<DocumentDto> UpdateDocumentAsync(int documentId, byte[] expectedRowVersion, UpdateDocumentDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await ValidateAsync(_updateValidator, dto);

        var entity = await _documentRepository.GetByIdAsync(documentId, track: true);

        if (entity is null)
        {
            throw new NotFoundException(ErrorMessages.DocumentNotFound, documentId.ToString(), nameof(Document));
        }

        if (!MatchesRowVersion(entity, expectedRowVersion))
        {
            _logger.LogWarning("RowVersion mismatch detected for document {DocumentId}", documentId);
            var currentEtag = entity.DokumentTimeStamp is null ? string.Empty : Convert.ToBase64String(entity.DokumentTimeStamp);
            var expectedEtag = Convert.ToBase64String(expectedRowVersion);
            throw new ConflictException(
                ErrorMessages.ConcurrencyConflict,
                documentId.ToString(),
                nameof(Document),
                expectedEtag,
                currentEtag);
        }

        _mapper.Map(dto, entity);

        _documentRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<DocumentDto>(entity);
    }

    public async Task<bool> DeleteDocumentAsync(int documentId)
    {
        var entity = await _documentRepository.GetByIdAsync(documentId, track: true);

        if (entity is null)
        {
            return false;
        }

        // Hard delete - remove from database
        _documentRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static bool MatchesRowVersion(Document entity, byte[] expectedRowVersion)
    {
        if (entity.DokumentTimeStamp is null)
        {
            return expectedRowVersion is null || expectedRowVersion.Length == 0;
        }

        return expectedRowVersion != null && entity.DokumentTimeStamp.SequenceEqual(expectedRowVersion);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance)
    {
        var validationResult = await validator.ValidateAsync(instance);
        if (validationResult.IsValid)
        {
            return;
        }

        var errors = validationResult.Errors
            .GroupBy(failure => failure.PropertyName ?? string.Empty)
            .ToDictionary(
                group => group.Key,
                group => group.Select(static failure => failure.ErrorMessage).ToArray());

        throw new ValidationException(ErrorMessages.ValidationFailed, errors);
    }
}
