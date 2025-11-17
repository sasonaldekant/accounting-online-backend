using System;

namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za ažuriranje zavisnog troška
/// </summary>
public record UpdateDocumentCostDto(
    int CostTypeId,
    decimal AmountNet,
    decimal AmountVat,
    DateTime DueDate,
    string? Description);
