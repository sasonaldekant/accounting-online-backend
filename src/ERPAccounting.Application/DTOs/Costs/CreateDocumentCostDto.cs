using System;

namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za kreiranje zavisnog troška
/// </summary>
public record CreateDocumentCostDto(
    int CostTypeId,
    decimal AmountNet,
    decimal AmountVat,
    DateTime DueDate,
    string? Description);
