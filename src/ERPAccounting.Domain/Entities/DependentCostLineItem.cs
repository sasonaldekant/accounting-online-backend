using System;
using System.ComponentModel.DataAnnotations;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// Dependent Cost Line Item (tblDokumentTroskoviStavka)
/// KRITIČNO: Sa RowVersion za ETag konkurentnost
/// </summary>
public class DependentCostLineItem : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DependentCostId { get; set; }
    
    public int CostTypeId { get; set; }
    public int DistributionMethodId { get; set; }
    public decimal Amount { get; set; }
    public bool CalculateTax { get; set; } = true;
    public int VatRate { get; set; } = 20;
    
    /// <summary>
    /// JSON array of article IDs for distribution: "[1,2,3]"
    /// </summary>
    public string ArticleIds { get; set; } = "[]";
    
    /// <summary>
    /// KRITIČNO: RowVersion za konkurentnost (ETag)
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation
    public virtual DocumentCost DependentCost { get; set; } = null!;
}
