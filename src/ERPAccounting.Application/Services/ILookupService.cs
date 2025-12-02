using ERPAccounting.Application.DTOs;

namespace ERPAccounting.Application.Services;

/// <summary>
/// Provides a controller-facing abstraction for all lookup-related operations.
/// Coordinates retrieval of lookup data from the persistence layer, applying
/// any defaulting or validation logic in the application layer.
/// </summary>
public interface ILookupService
{
    // ═══════════════════════════════════════════════════════════════
    // ORIGINAL METHODS (Stored Procedures via Gateway)
    // ═══════════════════════════════════════════════════════════════
    Task<List<PartnerComboDto>> GetPartnerComboAsync();
    Task<List<OrgUnitComboDto>> GetOrgUnitsComboAsync(string? docTypeId = null);
    Task<List<TaxationMethodComboDto>> GetTaxationMethodsComboAsync();
    Task<List<ReferentComboDto>> GetReferentsComboAsync();
    Task<List<DocumentNDComboDto>> GetDocumentNDComboAsync();
    Task<List<TaxRateComboDto>> GetTaxRatesComboAsync();
    Task<List<ArticleComboDto>> GetArticlesComboAsync();
    Task<List<DocumentCostsListDto>> GetDocumentCostsListAsync(int documentId);
    Task<List<CostTypeComboDto>> GetCostTypesComboAsync();
    Task<List<CostDistributionMethodComboDto>> GetCostDistributionMethodsComboAsync();
    Task<List<CostArticleComboDto>> GetCostArticlesComboAsync(int documentId);

    // ═══════════════════════════════════════════════════════════════
    // NEW METHODS - Server-Side Search (LINQ + EF Core)
    // ═══════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Search partners by code or name (server-side filtering).
    /// Used for autocomplete with large datasets (6000+ records).
    /// </summary>
    /// <param name="searchTerm">Search term (minimum 2 characters)</param>
    /// <param name="limit">Maximum number of results (default: 50, max: 100)</param>
    /// <returns>List of partners matching the search term</returns>
    Task<List<PartnerComboDto>> SearchPartnersAsync(string searchTerm, int limit = 50);
    
    /// <summary>
    /// Search articles by code or name (server-side filtering).
    /// Used for autocomplete with large datasets (11000+ records).
    /// </summary>
    /// <param name="searchTerm">Search term (minimum 2 characters)</param>
    /// <param name="limit">Maximum number of results (default: 50, max: 100)</param>
    /// <returns>List of articles matching the search term</returns>
    Task<List<ArticleComboDto>> SearchArticlesAsync(string searchTerm, int limit = 50);
}
