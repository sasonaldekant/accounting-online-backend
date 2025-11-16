using System;
using System.ComponentModel.DataAnnotations;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// Document Line Item (tblStavkaDokumenta)
/// KRITIČNO: Sa RowVersion za ETag konkurentnost
/// </summary>
public class DocumentLineItem : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    
    // Article info
    public int ArticleId { get; set; }
    
    // Pricing
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal Margin { get; set; } = 0;
    
    // Tax
    public int VatRate { get; set; } = 20;
    public bool CalculateExcise { get; set; } = false;
    public bool CalculateTax { get; set; } = true;
    
    /// <summary>
    /// KRITIČNO: RowVersion za konkurentnost (ETag)
    /// SQL Server: TIMESTAMP
    /// EF Core: [Timestamp] attribute
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation
    public virtual Document Document { get; set; } = null!;
}
