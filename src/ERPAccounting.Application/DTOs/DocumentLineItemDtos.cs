namespace ERPAccounting.Application.DTOs
{
    /// <summary>
    /// DTO za kreiranje nove stavke dokumenta (POST /documents/{id}/items)
    /// </summary>
    public record CreateLineItemDto(
        int ArticleId,
        decimal Quantity,
        decimal InvoicePrice,
        decimal? DiscountAmount = null,
        decimal? MarginAmount = null,
        string? TaxRateId = null,
        bool CalculateExcise = false,
        bool CalculateTax = true,
        string? Description = null,
        int? OrganizationalUnitId = null
    );

    /// <summary>
    /// DTO za ažuriranje stavke dokumenta (PATCH /documents/{id}/items/{itemId})
    /// SVA POLJA SU OPCIONA jer se koristi PATCH sa If-Match header-om za konkurentnost
    /// </summary>
    public record PatchLineItemDto(
        decimal? Quantity = null,
        decimal? InvoicePrice = null,
        decimal? DiscountAmount = null,
        decimal? MarginAmount = null,
        string? TaxRateId = null,
        bool? CalculateExcise = null,
        bool? CalculateTax = null,
        string? Description = null
    );

    /// <summary>
    /// DTO za prikaz stavke dokumenta (GET /documents/{id}/items/{itemId})
    /// Sadrži ETag za konkurentnost mehanizam
    /// </summary>
    public record DocumentLineItemDto(
        int Id,
        int DocumentId,
        int ArticleId,
        decimal Quantity,
        decimal InvoicePrice,
        decimal? DiscountAmount,
        decimal? MarginAmount,
        string? TaxRateId,
        decimal? TaxPercent,
        decimal? TaxAmount,
        decimal? Total,
        bool CalculateExcise,
        bool CalculateTax,
        string? Description,
        // ══════════════════════════════════════════════════
        // KONKURENTNOST - OBAVEZNO!
        /// <summary>ETag za If-Match header (Base64 RowVersion)</summary>
        string ETag,
        // ══════════════════════════════════════════════════
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int? CreatedBy,
        int? UpdatedBy
    );

    /// <summary>
    /// DTO za prikaz liste stavki bez detaljnih informa (GET /documents/{id}/items)
    /// </summary>
    public record DocumentLineItemListDto(
        int Id,
        int DocumentId,
        int ArticleId,
        decimal Quantity,
        decimal InvoicePrice,
        decimal? Total,
        decimal? TaxAmount,
        bool CalculateTax,
        string ETag,
        DateTime UpdatedAt
    );
}
