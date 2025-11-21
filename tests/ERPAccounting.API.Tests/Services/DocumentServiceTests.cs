using System;
using System.Collections.Generic;
using AutoMapper;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.DTOs.Documents;
using ERPAccounting.Application.Mapping;
using ERPAccounting.Application.Services;
using ERPAccounting.Common.Exceptions;
using ERPAccounting.Domain.Abstractions.Repositories;
using ERPAccounting.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using DomainValidationException = ERPAccounting.Common.Exceptions.ValidationException;

namespace ERPAccounting.API.Tests.Services;

public class DocumentServiceTests
{
    private static readonly CreateDocumentDto CreateDto = new(
        DocumentNumber: "UR-1",
        DocumentDate: new DateTime(2024, 1, 1),
        PartnerId: 1,
        OrganizationalUnitId: 2,
        ReferentDocumentId: null,
        DependentCostsNet: 10,
        DependentCostsVat: 2,
        Note: "test",
        Processed: false,
        Posted: false);

    private static readonly UpdateDocumentDto UpdateDto = new(
        DocumentNumber: "UR-1",
        DocumentDate: new DateTime(2024, 1, 2),
        PartnerId: 2,
        OrganizationalUnitId: 3,
        ReferentDocumentId: 5,
        DependentCostsNet: 12,
        DependentCostsVat: 3,
        Note: "updated",
        Processed: true,
        Posted: true);

    [Fact]
    public async Task CreateDocumentAsync_PersistsEntityAndReturnsDto()
    {
        var repositoryMock = new Mock<IDocumentRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var service = CreateService(repositoryMock, unitOfWorkMock);

        var result = await service.CreateDocumentAsync(CreateDto);

        repositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Document>(), default), Times.Once);
        unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(default), Times.Once);
        Assert.Equal(CreateDto.DocumentNumber, result.DocumentNumber);
    }

    [Fact]
    public async Task CreateDocumentAsync_InvalidDto_ThrowsValidationException()
    {
        var repositoryMock = new Mock<IDocumentRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var validator = CreateFailingValidator<CreateDocumentDto>();
        var service = CreateService(repositoryMock, unitOfWorkMock, createValidator: validator);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateDocumentAsync(CreateDto));
    }

    [Fact]
    public async Task GetDocumentsAsync_ReturnsPaginatedResult()
    {
        var repositoryMock = new Mock<IDocumentRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var documents = new List<Document>
        {
            new Document
            {
                IDDokument = 1,
                BrojDokumenta = "UR-1",
                Datum = new DateTime(2024, 1, 1),
                IDOrganizacionaJedinica = 2,
                ZavisniTroskoviBezPDVa = 10,
                ZavisniTroskoviPDV = 2,
                DokumentTimeStamp = new byte[] { 1 }
            }
        };
        repositoryMock.Setup(r => r.GetPaginatedAsync(1, 20, null, default))
            .ReturnsAsync((documents, documents.Count));

        var service = CreateService(repositoryMock, unitOfWorkMock);

        var result = await service.GetDocumentsAsync(new DocumentQueryParameters());

        Assert.Equal(documents.Count, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("UR-1", result.Items[0].DocumentNumber);
    }

    [Fact]
    public async Task UpdateDocumentAsync_RowVersionMismatch_ThrowsConflict()
    {
        var repositoryMock = new Mock<IDocumentRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, true, default)).ReturnsAsync(new Document
        {
            IDDokument = 1,
            BrojDokumenta = "UR-1",
            Datum = new DateTime(2024, 1, 1),
            IDOrganizacionaJedinica = 2,
            DokumentTimeStamp = new byte[] { 1, 2, 3 }
        });

        var service = CreateService(repositoryMock, unitOfWorkMock);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateDocumentAsync(1, new byte[] { 9, 9, 9 }, UpdateDto));
    }

    [Fact]
    public async Task UpdateDocumentAsync_RowVersionMatches_UpdatesEntity()
    {
        var repositoryMock = new Mock<IDocumentRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var rowVersion = new byte[] { 1, 2, 3 };
        var entity = new Document
        {
            IDDokument = 1,
            BrojDokumenta = "Old",
            Datum = new DateTime(2024, 1, 1),
            IDPartner = 10,
            IDOrganizacionaJedinica = 20,
            ZavisniTroskoviBezPDVa = 5,
            ZavisniTroskoviPDV = 1,
            Napomena = "old",
            ObradjenDokument = false,
            ProknjizenDokument = false,
            DokumentTimeStamp = rowVersion
        };
        repositoryMock.Setup(r => r.GetByIdAsync(1, true, default)).ReturnsAsync(entity);

        var service = CreateService(repositoryMock, unitOfWorkMock);

        var result = await service.UpdateDocumentAsync(1, rowVersion, UpdateDto);

        repositoryMock.Verify(r => r.Update(It.Is<Document>(d =>
            d.IDDokument == 1 &&
            d.BrojDokumenta == UpdateDto.DocumentNumber &&
            d.Datum == UpdateDto.DocumentDate &&
            d.IDPartner == UpdateDto.PartnerId &&
            d.IDOrganizacionaJedinica == UpdateDto.OrganizationalUnitId &&
            d.IDReferentniDokument == UpdateDto.ReferentDocumentId &&
            d.ZavisniTroskoviBezPDVa == UpdateDto.DependentCostsNet &&
            d.ZavisniTroskoviPDV == UpdateDto.DependentCostsVat &&
            d.Napomena == UpdateDto.Note &&
            d.ObradjenDokument == UpdateDto.Processed &&
            d.ProknjizenDokument == UpdateDto.Posted)), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);

        Assert.Equal(UpdateDto.DocumentNumber, result.DocumentNumber);
        Assert.Equal(UpdateDto.DocumentDate, result.DocumentDate);
        Assert.Equal(UpdateDto.PartnerId, result.PartnerId);
        Assert.Equal(UpdateDto.OrganizationalUnitId, result.OrganizationalUnitId);
        Assert.Equal(UpdateDto.ReferentDocumentId, result.ReferentDocumentId);
        Assert.Equal(UpdateDto.DependentCostsNet, result.DependentCostsNet);
        Assert.Equal(UpdateDto.DependentCostsVat, result.DependentCostsVat);
        Assert.Equal(UpdateDto.Note, result.Note);
        Assert.Equal(UpdateDto.Processed, result.Processed);
        Assert.Equal(UpdateDto.Posted, result.Posted);
    }

    [Fact]
    public async Task DeleteDocumentAsync_NotFound_ReturnsFalse()
    {
        var repositoryMock = new Mock<IDocumentRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, true, default)).ReturnsAsync((Document?)null);

        var service = CreateService(repositoryMock, unitOfWorkMock);

        var result = await service.DeleteDocumentAsync(1);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteDocumentAsync_ExistingEntity_MarksAsDeleted()
    {
        var repositoryMock = new Mock<IDocumentRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var entity = new Document { IDDokument = 1, IsDeleted = false };
        repositoryMock.Setup(r => r.GetByIdAsync(1, true, default)).ReturnsAsync(entity);

        var service = CreateService(repositoryMock, unitOfWorkMock);

        var result = await service.DeleteDocumentAsync(1);

        Assert.True(entity.IsDeleted);
        Assert.True(result);
        repositoryMock.Verify(r => r.Update(It.Is<Document>(d => d == entity && d.IsDeleted)), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    private static DocumentService CreateService(
        Mock<IDocumentRepository> repository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<IValidator<CreateDocumentDto>>? createValidator = null,
        Mock<IValidator<UpdateDocumentDto>>? updateValidator = null,
        Mock<IValidator<DocumentQueryParameters>>? queryValidator = null)
    {
        createValidator ??= CreatePassingValidator<CreateDocumentDto>();
        updateValidator ??= CreatePassingValidator<UpdateDocumentDto>();
        queryValidator ??= CreatePassingValidator<DocumentQueryParameters>();

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<DocumentMappingProfile>()).CreateMapper();
        var logger = Mock.Of<ILogger<DocumentService>>();

        return new DocumentService(
            repository.Object,
            unitOfWork.Object,
            createValidator.Object,
            updateValidator.Object,
            queryValidator.Object,
            mapper,
            logger);
    }

    private static Mock<IValidator<T>> CreatePassingValidator<T>()
    {
        var validator = new Mock<IValidator<T>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<T>(), default))
            .ReturnsAsync(new ValidationResult());
        return validator;
    }

    private static Mock<IValidator<T>> CreateFailingValidator<T>()
    {
        var validator = new Mock<IValidator<T>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<T>(), default))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("DocumentNumber", "Invalid")
            }));
        return validator;
    }
}
