using System.Collections.Generic;

namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za pokretanje raspodele troškova
/// </summary>
public record CostDistributionRequestDto(
    int DistributionMethodId,
    IReadOnlyDictionary<int, decimal>? ManualDistribution);
