using ERPAccounting.Domain.Lookups;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ERPAccounting.Domain.Abstractions.Gateways;

/// <summary>
/// Abstraction over stored procedures used by lookup services.
/// Provides a persistence-agnostic contract so the application layer
/// does not depend on EF Core or a specific database implementation.
/// </summary>
public interface IStoredProcedureGateway
{
    Task<List<PartnerLookup>> GetPartnerComboAsync();
    Task<List<OrgUnitLookup>> GetOrgUnitsComboAsync(string docTypeId);
    Task<List<TaxationMethodLookup>> GetTaxationMethodsComboAsync();
    Task<List<ReferentLookup>> GetReferentsComboAsync();
    Task<List<DocumentNDLookup>> GetDocumentNDComboAsync();
    Task<List<TaxRateLookup>> GetTaxRatesComboAsync();
    Task<List<ArticleLookup>> GetArticlesComboAsync();
    Task<List<DocumentCostLookup>> GetDocumentCostsListAsync(int documentId);
    Task<List<CostTypeLookup>> GetCostTypesComboAsync();
    Task<List<CostDistributionMethodLookup>> GetCostDistributionMethodsComboAsync();
    Task<List<CostArticleLookup>> GetCostArticlesComboAsync(int documentId);
}
