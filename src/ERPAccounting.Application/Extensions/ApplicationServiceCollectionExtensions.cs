using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.DTOs.Costs;
using ERPAccounting.Application.DTOs.Documents;
using ERPAccounting.Application.Mapping;
using ERPAccounting.Application.Services;
using ERPAccounting.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ERPAccounting.Application.Extensions;

/// <summary>
/// Registers application-layer services, validators and supporting infrastructure.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentCostService, DocumentCostService>();
        services.AddScoped<IDocumentLineItemService, DocumentLineItemService>();
        services.AddScoped<IStoredProcedureService, StoredProcedureService>();
        services.AddScoped<ILookupService, LookupService>();

        services.AddAutoMapper(typeof(DocumentMappingProfile).Assembly);

        services.AddScoped<IValidator<CreateDocumentDto>, CreateDocumentValidator>();
        services.AddScoped<IValidator<UpdateDocumentDto>, UpdateDocumentValidator>();
        services.AddScoped<IValidator<DocumentQueryParameters>, DocumentQueryParametersValidator>();
        services.AddScoped<IValidator<CreateLineItemDto>, CreateLineItemValidator>();
        services.AddScoped<IValidator<PatchLineItemDto>, PatchLineItemValidator>();
        services.AddScoped<IValidator<CreateDocumentCostDto>, CreateDocumentCostValidator>();
        services.AddScoped<IValidator<UpdateDocumentCostDto>, UpdateDocumentCostValidator>();
        services.AddScoped<IValidator<CreateDocumentCostItemDto>, CreateDocumentCostItemValidator>();
        services.AddScoped<IValidator<PatchDocumentCostItemDto>, PatchDocumentCostItemValidator>();

        return services;
    }
}
