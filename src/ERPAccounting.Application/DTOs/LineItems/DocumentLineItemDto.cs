using System;

namespace ERPAccounting.Application.DTOs.LineItems;

/// <summary>
/// DTO za response stavke dokumenta
/// KRITIČNO: Sa ETag property za konkurentnost
/// </summary>
public record DocumentLineItemDto(
    Guid Id,
    Guid DocumentId,
    int ArticleId,
    decimal Quantity,
    decimal Price,
    decimal Discount,
    decimal Margin,
    int VatRate,
    bool CalculateExcise,
    bool CalculateTax,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int? CreatedBy,
    int? UpdatedBy,
    string ETag  // Base64 encoded RowVersion
);
