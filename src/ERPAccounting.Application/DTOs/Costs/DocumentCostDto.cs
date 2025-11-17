using System;

namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za zaglavlje zavisnih troškova dokumenta
/// </summary>
public record DocumentCostDto(
    int Id,
    int DocumentId,
    int CostTypeId,
    string CostTypeName,
    decimal AmountNet,
    decimal AmountVat,
    DateTime DueDate,
    string? Description,
    string ETag);
