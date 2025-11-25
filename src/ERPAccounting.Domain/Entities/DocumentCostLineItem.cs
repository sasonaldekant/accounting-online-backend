using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// DocumentCostLineItem entity - maps to tblDokumentTroskoviStavka table.
/// Represents individual line items within a cost document (cost type, amount, distribution method).
/// </summary>
[Table("tblDokumentTroskoviStavka")]
public class DocumentCostLineItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("IDDokumentTroskoviStavka")]
    public int IDDokumentTroskoviStavka { get; set; }
    
    [Required, Column("IDDokumentTroskovi")]
    public int IDDokumentTroskovi { get; set; }
    
    [Required, Column("IDNacinDeljenjaTroskova")]
    public int IDNacinDeljenjaTroskova { get; set; }
    
    [Column("SveStavke")]
    public bool SveStavke { get; set; } = true;
    
    [Column("Iznos", TypeName = "money")]
    public decimal Iznos { get; set; } = 0;
    
    [Required, Column("IDUlazniRacuniIzvedeni")]
    public int IDUlazniRacuniIzvedeni { get; set; }
    
    [Required, Column("IDStatus")]
    public int IDStatus { get; set; }
    
    [Column("ObracunPorezTroskovi")]
    public int ObracunPorezTroskovi { get; set; } = 0;
    
    [Column("DodajPDVNaTroskove")]
    public int DodajPDVNaTroskove { get; set; } = 0;

    /// <summary>
    /// CRITICAL: RowVersion for ETag concurrency control.
    /// This timestamp is automatically updated by SQL Server on every UPDATE.
    /// Used for optimistic concurrency detection.
    /// </summary>
    [Timestamp, Column("DokumentTroskoviStavkaTimeStamp")]
    public byte[]? DokumentTroskoviStavkaTimeStamp { get; set; }
    
    [Column("IznosValuta", TypeName = "money")]
    public decimal? IznosValuta { get; set; } = 0;
    
    [Column("Gotovina", TypeName = "money")]
    public decimal Gotovina { get; set; } = 0;
    
    [Column("Kartica", TypeName = "money")]
    public decimal Kartica { get; set; } = 0;
    
    [Column("Virman", TypeName = "money")]
    public decimal Virman { get; set; } = 0;
    
    [Column("Kolicina", TypeName = "money")]
    public decimal? Kolicina { get; set; } = 0;
    
    // Navigation properties
    public virtual DocumentCost DocumentCost { get; set; } = null!;
    public virtual ICollection<DocumentCostVAT> VATItems { get; set; } = new List<DocumentCostVAT>();
}
