namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO sa povratnom informacijom nakon raspodele troškova
/// </summary>
public record CostDistributionResultDto(
    int DocumentCostId,
    int ProcessedItems,
    decimal DistributedAmount);
