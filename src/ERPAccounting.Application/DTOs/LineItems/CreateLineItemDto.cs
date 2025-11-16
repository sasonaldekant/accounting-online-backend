namespace ERPAccounting.Application.DTOs.LineItems;

/// <summary>
/// DTO za kreiranje nove stavke
/// </summary>
public record CreateLineItemDto(
    int ArticleId,
    decimal Quantity,
    decimal Price,
    decimal Discount,
    decimal Margin,
    int VatRate,
    bool CalculateExcise,
    bool CalculateTax
);
