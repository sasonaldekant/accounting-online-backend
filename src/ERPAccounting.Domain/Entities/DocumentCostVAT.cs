using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// DocumentCostVAT entity - maps to tblDokumentTroskoviStavkaPDV table.
/// Represents VAT breakdown for cost line items.
/// </summary>
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
    
    /// <summary>
    /// CRITICAL: RowVersion for ETag concurrency control.
    /// This timestamp is automatically updated by SQL Server on every UPDATE.
    /// </summary>
    [Timestamp, Column("DokumentTroskoviStavkaPDVTimeStamp")]
    public byte[]? DokumentTroskoviStavkaPDVTimeStamp { get; set; }
    
    // Navigation property
    public virtual DocumentCostLineItem DocumentCostLineItem { get; set; } = null!;
}
