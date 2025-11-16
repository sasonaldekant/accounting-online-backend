using System;
using System.Collections.Generic;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// Document (tblDokument) - Header entity
/// </summary>
public class Document : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Required fields
    public string BrojDokumenta { get; set; } = string.Empty;
    public DateTime Datum { get; set; }
    public int PartnerId { get; set; }
    public int OrganizacionaJedinicaId { get; set; }
    public int RadnikId { get; set; }
    public int ValutaId { get; set; }
    public decimal KursValute { get; set; } = 1.0m;
    public int NacinOporezivanjaId { get; set; }
    
    // Optional fields
    public Guid? ReferentniDokumentId { get; set; }
    public string? Napomena { get; set; }
    
    // Business flags
    public bool ObracunAkciza { get; set; } = false;
    public bool ObracunPorez { get; set; } = true;
    public bool Procesiran { get; set; } = false;
    
    // Navigation properties
    public virtual ICollection<DocumentLineItem> LineItems { get; set; } = new List<DocumentLineItem>();
    public virtual ICollection<DocumentCost> Costs { get; set; } = new List<DocumentCost>();
}
