using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPAccounting.Domain.Entities;

[Table("tblDokumentTroskovi")]
public class DocumentCost
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("IDDokumentTroskovi")]
    public int IDDokumentTroskovi { get; set; }
    
    [Required, Column("IDDokument")]
    public int IDDokument { get; set; }
    
    [Required, Column("IDPartner")]
    public int IDPartner { get; set; }
    
    [Required, Column("IDVrstaDokumenta"), StringLength(2)]
    public string IDVrstaDokumenta { get; set; } = string.Empty;
    
    [Required, Column("BrojDokumenta")]
    public string BrojDokumenta { get; set; } = string.Empty;
    
    [Required, Column("DatumDPO")]
    public DateTime DatumDPO { get; set; }
    
    [Column("DatumValute")]
    public DateTime? DatumValute { get; set; }
    
    [Column("Opis")]
    public string? Opis { get; set; }
    
    [Required, Column("IDStatus")]
    public int IDStatus { get; set; }
    
    [Column("IDValuta")]
    public int? IDValuta { get; set; }
    
    [Column("Kurs", TypeName = "money")]
    public decimal? Kurs { get; set; } = 0;
    
    /// <summary>CRITICAL: RowVersion for ETag concurrency</summary>
    [Timestamp, Column("DokumentTroskoviTimeStamp")]
    public byte[]? DokumentTroskoviTimeStamp { get; set; }
    
    // Navigation
    public virtual Document Document { get; set; } = null!;
    public virtual ICollection<DocumentCostLineItem> LineItems { get; set; } = new List<DocumentCostLineItem>();
}
