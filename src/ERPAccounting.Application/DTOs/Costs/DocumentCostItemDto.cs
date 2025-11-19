namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za stavke zavisnih troškova sa ETag vrednošću
/// </summary>
public record DocumentCostItemDto(
    int Id,
    int DocumentCostId,
    int ArticleId,
    decimal Quantity,
    decimal AmountNet,
    decimal AmountVat,
    int TaxRateId,
    string? Note,
    string ETag);
