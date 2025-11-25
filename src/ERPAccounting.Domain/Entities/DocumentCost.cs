using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// DocumentCost entity - maps to tblDokumentTroskovi table.
/// Represents dependent costs associated with a document (shipping, customs, insurance, etc.)
/// </summary>
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

    /// <summary>
    /// CRITICAL: RowVersion for ETag concurrency control.
    /// This timestamp is automatically updated by SQL Server on every UPDATE.
    /// Used for optimistic concurrency detection.
    /// </summary>
    [Timestamp, Column("DokumentTroskoviTimeStamp")]
    public byte[]? DokumentTroskoviTimeStamp { get; set; }
    
    // Navigation properties
    public virtual Document Document { get; set; } = null!;
    public virtual ICollection<DocumentCostLineItem> CostLineItems { get; set; } = new List<DocumentCostLineItem>();

    // ═══════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES - calculated from child line items
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Ukupan iznos bez PDV (suma svih stavki).
    /// NOTE: This is NOT a database column - it's computed from CostLineItems.
    /// </summary>
    [NotMapped]
    public decimal IznosBezPDV => CostLineItems?.Sum(item => item.Iznos) ?? 0;

    /// <summary>
    /// Ukupan iznos PDV (suma svih PDV iz stavki).
    /// NOTE: This is NOT a database column - it's computed from CostLineItems VAT totals.
    /// </summary>
    [NotMapped]
    public decimal IznosPDV
    {
        get
        {
            if (CostLineItems == null || !CostLineItems.Any())
                return 0;

            return CostLineItems.Sum(item =>
                item.VATItems?.Sum(vat => vat.IznosPDV) ?? 0
            );
        }
    }
}
