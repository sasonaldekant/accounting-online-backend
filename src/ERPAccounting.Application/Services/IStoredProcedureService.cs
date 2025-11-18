using ERPAccounting.Application.DTOs;

namespace ERPAccounting.Application.Services;

/// <summary>
/// Application layer abstraction over the stored procedure gateway.
/// Allows services to consume lookup data without depending on EF Core details.
/// </summary>
public interface IStoredProcedureService
{
    Task<List<PartnerComboDto>> GetPartnerComboAsync();
    Task<List<OrgUnitComboDto>> GetOrgUnitsComboAsync(string docTypeId);
    Task<List<TaxationMethodComboDto>> GetTaxationMethodsComboAsync();
    Task<List<ReferentComboDto>> GetReferentsComboAsync();
    Task<List<DocumentNDComboDto>> GetDocumentNDComboAsync();
    Task<List<TaxRateComboDto>> GetTaxRatesComboAsync();
    Task<List<ArticleComboDto>> GetArticlesComboAsync();
    Task<List<DocumentCostsListDto>> GetDocumentCostsListAsync(int documentId);
    Task<List<CostTypeComboDto>> GetCostTypesComboAsync();
    Task<List<CostDistributionMethodComboDto>> GetCostDistributionMethodsComboAsync();
    Task<List<CostArticleComboDto>> GetCostArticlesComboAsync(int documentId);
}
