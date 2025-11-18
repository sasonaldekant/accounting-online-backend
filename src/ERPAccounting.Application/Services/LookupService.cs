using ERPAccounting.Application.DTOs;

namespace ERPAccounting.Application.Services;

/// <summary>
/// High level lookup orchestrator that is consumed by API controllers.
/// Keeps controller logic thin by coordinating with <see cref="IStoredProcedureService"/>.
/// </summary>
public class LookupService : ILookupService
{
    private const string DefaultDocumentTypeId = "UR";
    private readonly IStoredProcedureService _storedProcedureService;

    public LookupService(IStoredProcedureService storedProcedureService)
    {
        _storedProcedureService = storedProcedureService;
    }

    public Task<List<PartnerComboDto>> GetPartnerComboAsync()
        => _storedProcedureService.GetPartnerComboAsync();

    public Task<List<OrgUnitComboDto>> GetOrgUnitsComboAsync(string? docTypeId = null)
    {
        var effectiveDocTypeId = string.IsNullOrWhiteSpace(docTypeId)
            ? DefaultDocumentTypeId
            : docTypeId;

        return _storedProcedureService.GetOrgUnitsComboAsync(effectiveDocTypeId);
    }

    public Task<List<TaxationMethodComboDto>> GetTaxationMethodsComboAsync()
        => _storedProcedureService.GetTaxationMethodsComboAsync();

    public Task<List<ReferentComboDto>> GetReferentsComboAsync()
        => _storedProcedureService.GetReferentsComboAsync();

    public Task<List<DocumentNDComboDto>> GetDocumentNDComboAsync()
        => _storedProcedureService.GetDocumentNDComboAsync();

    public Task<List<TaxRateComboDto>> GetTaxRatesComboAsync()
        => _storedProcedureService.GetTaxRatesComboAsync();

    public Task<List<ArticleComboDto>> GetArticlesComboAsync()
        => _storedProcedureService.GetArticlesComboAsync();

    public Task<List<DocumentCostsListDto>> GetDocumentCostsListAsync(int documentId)
        => _storedProcedureService.GetDocumentCostsListAsync(documentId);

    public Task<List<CostTypeComboDto>> GetCostTypesComboAsync()
        => _storedProcedureService.GetCostTypesComboAsync();

    public Task<List<CostDistributionMethodComboDto>> GetCostDistributionMethodsComboAsync()
        => _storedProcedureService.GetCostDistributionMethodsComboAsync();

    public Task<List<CostArticleComboDto>> GetCostArticlesComboAsync(int documentId)
        => _storedProcedureService.GetCostArticlesComboAsync(documentId);
}
