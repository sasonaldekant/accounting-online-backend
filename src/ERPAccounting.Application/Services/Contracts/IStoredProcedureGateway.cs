using ERPAccounting.Application.DTOs;

namespace ERPAccounting.Application.Services.Contracts;

/// <summary>
/// Abstraction over stored procedures used by lookup services.
/// Provides a persistence-agnostic contract so the application layer
/// does not depend on EF Core or a specific database implementation.
/// </summary>
public interface IStoredProcedureGateway
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
