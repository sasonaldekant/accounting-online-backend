using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// DocumentAdvanceVAT entity - maps to tblDokumentAvansPDV table.
/// Represents VAT on advance payments.
/// </summary>
[Table("tblDokumentAvansPDV")]
public class DocumentAdvanceVAT
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("DokumentAvansPDV")]
    public int DokumentAvansPDV { get; set; }
    
    [Required, Column("IDDokument")]
    public int IDDokument { get; set; }
    
    [Required, Column("IDPoreskaStopa"), StringLength(2)]
    public string IDPoreskaStopa { get; set; } = string.Empty;
    
    [Required, Column("IznosPDVAvansa", TypeName = "money")]
    public decimal IznosPDVAvansa { get; set; }
    
    [Column("BrojAvansa"), StringLength(50)]
    public string? BrojAvansa { get; set; }
    
    [Column("DatumAvansa")]
    public DateTime? DatumAvansa { get; set; }
    
    [Column("OsnovicaPoStopi", TypeName = "money")]
    public decimal? OsnovicaPoStopi { get; set; }
    
    [Column("KodOslobodjenja"), StringLength(50)]
    public string? KodOslobodjenja { get; set; }
    
    /// <summary>
    /// CRITICAL: RowVersion for ETag concurrency control.
    /// This timestamp is automatically updated by SQL Server on every UPDATE.
    /// </summary>
    [Timestamp, Column("DokumentAvansPDVTimeStamp")]
    public byte[]? DokumentAvansPDVTimeStamp { get; set; }
    
    // Navigation property
    public virtual Document Document { get; set; } = null!;
}
