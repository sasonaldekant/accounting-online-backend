namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za kreiranje stavke zavisnih troškova
/// </summary>
public record CreateDocumentCostItemDto(
    int ArticleId,
    decimal Quantity,
    decimal AmountNet,
    decimal AmountVat,
    int TaxRateId,
    string? Note);
