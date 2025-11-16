using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPAccounting.Domain.Entities;

[Table("tblStavkaDokumenta")]
public class DocumentLineItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("IDStavkaDokumenta")]
    public int IDStavkaDokumenta { get; set; }
    
    [Required, Column("IDDokument")]
    public int IDDokument { get; set; }
    
    [Required, Column("IDArtikal")]
    public int IDArtikal { get; set; }
    
    [Column("IDOrganizacionaJedinica")]
    public int? IDOrganizacionaJedinica { get; set; }
    
    [Required, Column("Kolicina", TypeName = "money")]
    public decimal Kolicina { get; set; }
    
    [Column("FakturnaCena", TypeName = "money")]
    public decimal FakturnaCena { get; set; } = 0;
    
    [Column("NabavnaCena", TypeName = "money")]
    public decimal NabavnaCena { get; set; } = 0;
    
    [Column("MagacinskaCena", TypeName = "money")]
    public decimal MagacinskaCena { get; set; } = 0;
    
    [Column("RabatDokument", TypeName = "money")]
    public decimal RabatDokument { get; set; } = 0;
    
    [Column("ProcenatAktivneMaterije", TypeName = "money")]
    public decimal ProcenatAktivneMaterije { get; set; } = 0;
    
    [Column("Zapremina", TypeName = "money")]
    public decimal Zapremina { get; set; } = 0;
    
    [Column("Akciza", TypeName = "money")]
    public decimal Akciza { get; set; } = 0;
    
    [Column("KoeficijentKolicine", TypeName = "money")]
    public decimal KoeficijentKolicine { get; set; } = 1;
    
    [Column("Rabat", TypeName = "money")]
    public decimal Rabat { get; set; } = 0;
    
    [Column("Marza", TypeName = "money")]
    public decimal Marza { get; set; } = 0;
    
    [Column("IznosMarze", TypeName = "money")]
    public decimal IznosMarze { get; set; } = 0;
    
    [Column("ProcenatPoreza", TypeName = "money")]
    public decimal ProcenatPoreza { get; set; } = 0;
    
    [Column("ProcenatPorezaMP", TypeName = "money")]
    public decimal ProcenatPorezaMP { get; set; } = 0;
    
    [Column("IznosPDV", TypeName = "money")]
    public decimal IznosPDV { get; set; } = 0;
    
    [Column("IznosPDVsaAkcizom", TypeName = "money")]
    public decimal IznosPDVsaAkcizom { get; set; } = 0;
    
    [Column("IznosAkciza", TypeName = "money")]
    public decimal IznosAkciza { get; set; } = 0;
    
    [Column("IDPoreskaStopa"), StringLength(2)]
    public string? IDPoreskaStopa { get; set; }
    
    [Column("ZavisniTroskovi", TypeName = "money")]
    public decimal ZavisniTroskovi { get; set; } = 0;
    
    [Column("ZavisniTroskoviBezPoreza", TypeName = "money")]
    public decimal ZavisniTroskoviBezPoreza { get; set; } = 0;
    
    [Column("Iznos", TypeName = "money")]
    public decimal Iznos { get; set; } = 0;
    
    [Column("ValutaCena", TypeName = "money")]
    public decimal ValutaCena { get; set; } = 0;
    
    [Column("ValutaIznos", TypeName = "money")]
    public decimal ValutaIznos { get; set; } = 0;
    
    [Required, Column("IDJedinicaMere"), StringLength(6)]
    public string IDJedinicaMere { get; set; } = string.Empty;
    
    [Column("Pakovanje")]
    public int Pakovanje { get; set; } = 0;
    
    [Column("ObracunAkciza")]
    public short ObracunAkciza { get; set; } = 0;
    
    [Column("ObracunPorez")]
    public short ObracunPorez { get; set; } = 0;
    
    [Column("IDNacinOporezivanja")]
    public int? IDNacinOporezivanja { get; set; }
    
    [Column("IDStatus")]
    public int? IDStatus { get; set; }
    
    // Computed columns
    [Column("VrednostObracunPDV")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal? VrednostObracunPDV { get; private set; }
    
    [Column("VrednostObracunAkciza")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal? VrednostObracunAkciza { get; private set; }
    
    [Column("Masa", TypeName = "money")]
    public decimal Masa { get; set; } = 0;
    
    [Column("Opis"), StringLength(1024)]
    public string? Opis { get; set; }
    
    [Column("ProizvodnjaKolicina")]
    public double ProizvodnjaKolicina { get; set; } = 0;
    
    [Column("ProizvodnjaIDJedinicaMere"), StringLength(6)]
    public string? ProizvodnjaIDJedinicaMere { get; set; }
    
    [Column("ProizvodnjaKoeficijentKolicine")]
    public double ProizvodnjaKoeficijentKolicine { get; set; } = 0;
    
    [Column("IDObrociNarudzbinaStavka")]
    public int? IDObrociNarudzbinaStavka { get; set; }
    
    [Column("IDVrstaObroka")]
    public int? IDVrstaObroka { get; set; }
    
    /// <summary>CRITICAL: RowVersion for ETag concurrency</summary>
    [Timestamp, Column("StavkaDokumentaTimeStamp")]
    public byte[]? StavkaDokumentaTimeStamp { get; set; }
    
    [Column("IDDnevnaStanjaMagacinskoPromeneM1")]
    public int IDDnevnaStanjaMagacinskoPromeneM1 { get; set; } = 0;
    
    [Column("IDDnevnaStanjaMagacinskoPromeneM2")]
    public int IDDnevnaStanjaMagacinskoPromeneM2 { get; set; } = 0;
    
    [Column("IDDnevnaStanjaRobnoPromeneM1")]
    public int IDDnevnaStanjaRobnoPromeneM1 { get; set; } = 0;
    
    [Column("IDDnevnaStanjaRobnoPromeneM2")]
    public int IDDnevnaStanjaRobnoPromeneM2 { get; set; } = 0;
    
    [Column("IDDnevnaStanjaVPPromeneM1")]
    public int IDDnevnaStanjaVPPromeneM1 { get; set; } = 0;
    
    [Column("IDDnevnaStanjaVPPromeneM2")]
    public int IDDnevnaStanjaVPPromeneM2 { get; set; } = 0;
    
    [Column("ObracunPorezPomocni")]
    public short ObracunPorezPomocni { get; set; } = 0;
    
    [Column("IDUlazniRacuniOsnovni")]
    public int? IDUlazniRacuniOsnovni { get; set; }
    
    [Column("RabatAkcija", TypeName = "money")]
    public decimal RabatAkcija { get; set; } = 0;
    
    [Column("IsporukaRobe")]
    public bool? IsporukaRobe { get; set; }
    
    [Column("Rabat2", TypeName = "money")]
    public decimal Rabat2 { get; set; } = 0;
    
    [Column("ZadnjaNabavnaCena", TypeName = "money")]
    public decimal? ZadnjaNabavnaCena { get; set; } = 0;
    
    [Column("ProsecnaCena", TypeName = "money")]
    public decimal? ProsecnaCena { get; set; } = 0;
    
    [Column("ValutaBrojDana")]
    public int? ValutaBrojDana { get; set; }
    
    [Column("ValutaDatum")]
    public DateTime? ValutaDatum { get; set; }
    
    [Column("VrednostBezPDV", TypeName = "money")]
    public decimal? VrednostBezPDV { get; set; } = 0;
    
    [Column("ObaveznaOprema"), StringLength(50)]
    public string? ObaveznaOprema { get; set; }
    
    [Column("DopunskaOprema"), StringLength(50)]
    public string? DopunskaOprema { get; set; }
    
    [Column("ProsecnaCenaOJ", TypeName = "money")]
    public decimal? ProsecnaCenaOJ { get; set; }
    
    [Column("PovratnaNaknada", TypeName = "money")]
    public decimal? PovratnaNaknada { get; set; } = 0;
    
    [Column("StaraCena", TypeName = "money")]
    public decimal? StaraCena { get; set; }
    
    [Column("IDBoja")]
    public int? IDBoja { get; set; }
    
    // Navigation
    public virtual Document Document { get; set; } = null!;
}
