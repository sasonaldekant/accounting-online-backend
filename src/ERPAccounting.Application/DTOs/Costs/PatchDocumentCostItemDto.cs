namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za parcijalno ažuriranje stavke troška (PATCH)
/// </summary>
public record PatchDocumentCostItemDto(
    decimal? Quantity,
    decimal? AmountNet,
    decimal? AmountVat,
    int? TaxRateId,
    string? Note);
