using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPAccounting.Domain.Entities;

[Table("tblDokumentTroskoviStavka")]
public class DocumentCostLineItem : BaseEntity
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

    [Column("Napomena")]
    public string? Napomena { get; set; }

    /// <summary>CRITICAL: RowVersion for ETag concurrency</summary>
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
    
    // Navigation
    public virtual DocumentCost DocumentCost { get; set; } = null!;
    public virtual ICollection<DocumentCostVAT> VATItems { get; set; } = new List<DocumentCostVAT>();
}
