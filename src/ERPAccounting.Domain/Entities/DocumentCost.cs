using System;
using System.Collections.Generic;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// Document Cost (tblDokumentTroskovi) - Dependent cost header
/// </summary>
public class DocumentCost : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    
    public int PartnerId { get; set; }
    public int VrstaDokumentaId { get; set; }
    public string BrojDokumenta { get; set; } = string.Empty;
    public DateTime DatumDPO { get; set; }
    public DateTime DatumValute { get; set; }
    public decimal Kurs { get; set; } = 1.0m;
    public string? Opis { get; set; }
    
    // Navigation
    public virtual Document Document { get; set; } = null!;
    public virtual ICollection<DependentCostLineItem> LineItems { get; set; } = new List<DependentCostLineItem>();
}
