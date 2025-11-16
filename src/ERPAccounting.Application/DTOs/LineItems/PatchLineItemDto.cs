namespace ERPAccounting.Application.DTOs.LineItems;

/// <summary>
/// DTO za PATCH (autosave) postojeće stavke
/// KRITIČNO: Koristi se za Excel-like autosave sa ETag konkurentnosti
/// </summary>
public record PatchLineItemDto(
    decimal Quantity,
    decimal Price,
    decimal Discount,
    decimal Margin,
    int VatRate,
    bool CalculateExcise,
    bool CalculateTax
);
