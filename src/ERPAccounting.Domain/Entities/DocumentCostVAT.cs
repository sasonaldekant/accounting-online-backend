using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPAccounting.Domain.Entities;

[Table("tblDokumentTroskoviStavkaPDV")]
public class DocumentCostVAT
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("IDDokumentTroskoviStavkaPDV")]
    public int IDDokumentTroskoviStavkaPDV { get; set; }
    
    [Required, Column("IDDokumentTroskoviStavka")]
    public int IDDokumentTroskoviStavka { get; set; }
    
    [Required, Column("IDPoreskaStopa"), StringLength(2)]
    public string IDPoreskaStopa { get; set; } = string.Empty;
    
    [Column("IznosPDV", TypeName = "money")]
    public decimal IznosPDV { get; set; } = 0;
    
    /// <summary>CRITICAL: RowVersion for ETag concurrency</summary>
    [Timestamp, Column("DokumentTroskoviStavkaPDVTimeStamp")]
    public byte[]? DokumentTroskoviStavkaPDVTimeStamp { get; set; }
    
    // Navigation
    public virtual DocumentCostLineItem DocumentCostLineItem { get; set; } = null!;
}
