using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERPAccounting.Domain.Interfaces;

namespace ERPAccounting.Domain.Entities;

[Table("tblDokument")]
public class Document : BaseEntity, ISoftDeletable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("IDDokument")]
    public int IDDokument { get; set; }
    
    [Required, Column("IDVrstaDokumenta"), StringLength(2)]
    public string IDVrstaDokumenta { get; set; } = string.Empty;
    
    [Required, Column("BrojDokumenta"), StringLength(30)]
    public string BrojDokumenta { get; set; } = string.Empty;
    
    [Column("BrojDokumentaINT")]
    public int BrojDokumentaINT { get; set; } = 0;
    
    [Column("Godina")]
    public int? Godina { get; set; }
    
    [Required, Column("Datum")]
    public DateTime Datum { get; set; }
    
    [Column("IDPartner")]
    public int? IDPartner { get; set; }
    
    [Required, Column("IDOrganizacionaJedinica")]
    public int IDOrganizacionaJedinica { get; set; }
    
    [Column("IDInterniPartner")]
    public int? IDInterniPartner { get; set; }
    
    [Column("DatumValute")]
    public DateTime? DatumValute { get; set; }
    
    [Column("DatumDPO")]
    public DateTime? DatumDPO { get; set; }
    
    [Column("PartnerBrojDokumenta"), StringLength(200)]
    public string? PartnerBrojDokumenta { get; set; }
    
    [Column("PartnerDatumDokumenta")]
    public DateTime? PartnerDatumDokumenta { get; set; }
    
    [Column("IDRadnik")]
    public int? IDRadnik { get; set; }
    
    [Column("IDReferentniDokument")]
    public int? IDReferentniDokument { get; set; }
    
    [Column("Napomena")]
    public string? Napomena { get; set; }
    
    [Column("NapomenaSystem")]
    public string? NapomenaSystem { get; set; }
    
    [Column("ObradjenDokument")]
    public bool ObradjenDokument { get; set; } = false;
    
    [Column("ProknjizenDokument")]
    public bool ProknjizenDokument { get; set; } = false;
    
    [Column("UserName"), StringLength(20)]
    public string? UserName { get; set; }
    
    [Column("UserLokacija"), StringLength(30)]
    public string? UserLokacija { get; set; }
    
    [Column("UserDatum")]
    public DateTime? UserDatum { get; set; }
    
    [Column("IDNacinPlacanja")]
    public int? IDNacinPlacanja { get; set; }
    
    [Column("IDNacinOporezivanja")]
    public int? IDNacinOporezivanja { get; set; }
    
    [Column("IDStatus")]
    public int? IDStatus { get; set; }
    
    [Column("ObracunAkciza")]
    public short ObracunAkciza { get; set; } = 0;
    
    [Column("ObracunPorez")]
    public short ObracunPorez { get; set; } = 0;
    
    [Column("ObracunPorezPomocni")]
    public short ObracunPorezPomocni { get; set; } = 0;
    
    [Column("IDValuta")]
    public int? IDValuta { get; set; }
    
    [Required, Column("KursValute", TypeName = "money")]
    public decimal KursValute { get; set; }
    
    [Column("AvansIznos", TypeName = "money")]
    public decimal AvansIznos { get; set; } = 0;
    
    [Column("IDModelKontiranja")]
    public int? IDModelKontiranja { get; set; }
    
    [Column("IDMestoIsporuke")]
    public int? IDMestoIsporuke { get; set; }
    
    [Column("TrebovanjeIDArtikal")]
    public int? TrebovanjeIDArtikal { get; set; }
    
    [Column("TrebovanjeKolicina", TypeName = "money")]
    public decimal TrebovanjeKolicina { get; set; } = 0;
    
    [Column("IznosPrevaranti", TypeName = "money")]
    public decimal IznosPrevaranti { get; set; } = 0;

    public bool IsDeleted { get; set; }
    
    [Column("ZavisniTroskoviBezPDVa", TypeName = "money")]
    public decimal ZavisniTroskoviBezPDVa { get; set; } = 0;
    
    [Column("ZavisniTroskoviPDV", TypeName = "money")]
    public decimal ZavisniTroskoviPDV { get; set; } = 0;
    
    [Column("IDTroskovnoMesto")]
    public int? IDTroskovnoMesto { get; set; }
    
    [Column("IDVozac")]
    public int? IDVozac { get; set; }
    
    [Column("IDVozilo")]
    public int? IDVozilo { get; set; }
    
    [Column("IDLinijaProizvodnje")]
    public int? IDLinijaProizvodnje { get; set; }
    
    [Column("IDSvrhaInternihRacuna")]
    public int? IDSvrhaInternihRacuna { get; set; }
    
    [Column("UserNameK"), StringLength(30)]
    public string? UserNameK { get; set; }
    
    [Column("UserLokacijaK"), StringLength(30)]
    public string? UserLokacijaK { get; set; }
    
    [Column("UserDatumK")]
    public DateTime? UserDatumK { get; set; }
    
    [Column("Bruto", TypeName = "money")]
    public decimal? Bruto { get; set; }
    
    [Column("Neto", TypeName = "money")]
    public decimal? Neto { get; set; }
    
    [Column("GranicniPrelaz"), StringLength(200)]
    public string? GranicniPrelaz { get; set; }
    
    [Column("IDStorniranogDokumenta")]
    public int? IDStorniranogDokumenta { get; set; }
    
    [Column("IDUlazniRacuniOsnovni")]
    public int? IDUlazniRacuniOsnovni { get; set; }
    
    [Column("IznosCek", TypeName = "money")]
    public decimal IznosCek { get; set; } = 0;
    
    [Column("IznosKartica", TypeName = "money")]
    public decimal IznosKartica { get; set; } = 0;
    
    [Column("IznosGotovina", TypeName = "money")]
    public decimal IznosGotovina { get; set; } = 0;
    
    [Column("BrojPutnogNaloga"), StringLength(50)]
    public string? BrojPutnogNaloga { get; set; }
    
    [Column("Otpremljeno")]
    public bool? Otpremljeno { get; set; }
    
    [Column("VremeRazvoza"), StringLength(50)]
    public string? VremeRazvoza { get; set; }
    
    [Column("BrojDokAlt")]
    public string? BrojDokAlt { get; set; }
    
    [Column("Napomena2")]
    public string? Napomena2 { get; set; }
    
    [Column("Napomena3")]
    public string? Napomena3 { get; set; }
    
    [Column("SinhronizovanAccess")]
    public bool SinhronizovanAccess { get; set; } = false;
    
    [Column("Feler")]
    public bool Feler { get; set; } = false;
    
    [Column("IndikatorNaknadnogOdobrenja"), StringLength(1)]
    public string? IndikatorNaknadnogOdobrenja { get; set; }
    
    [Column("OdobrioNaknadnuIsporuku"), StringLength(30)]
    public string? OdobrioNaknadnuIsporuku { get; set; }
    
    [Column("ImePrezimeMetro"), StringLength(50)]
    public string? ImePrezimeMetro { get; set; }
    
    [Column("BrojNarudzbenice"), StringLength(50)]
    public string? BrojNarudzbenice { get; set; }
    
    [Column("BrojProdavnice"), StringLength(50)]
    public string? BrojProdavnice { get; set; }
    
    [Column("DatumNarudzbenice")]
    public DateTime? DatumNarudzbenice { get; set; }
    
    [Column("IDTekuciRacun")]
    public int? IDTekuciRacun { get; set; }
    
    [Column("PozivNaBroj"), StringLength(50)]
    public string? PozivNaBroj { get; set; }
    
    [Column("VrednostSaRacuna", TypeName = "money")]
    public decimal? VrednostSaRacuna { get; set; }
    
    [Column("PozivNaBroj1"), StringLength(50)]
    public string? PozivNaBroj1 { get; set; }
    
    /// <summary>CRITICAL: RowVersion for ETag concurrency</summary>
    [Timestamp, Column("DokumentTimeStamp")]
    public byte[] DokumentTimeStamp { get; set; } = Array.Empty<byte>();
    
    [Column("Rok")]
    public DateTime? Rok { get; set; }
    
    [Column("Kilometraza", TypeName = "money")]
    public decimal? Kilometraza { get; set; }
    
    [Column("Kontakt"), StringLength(50)]
    public string? Kontakt { get; set; }
    
    [Column("Registracija"), StringLength(50)]
    public string? Registracija { get; set; }
    
    [Column("IDRadnik2")]
    public int? IDRadnik2 { get; set; }
    
    [Column("DodatniRadoviIznos", TypeName = "money")]
    public decimal? DodatniRadoviIznos { get; set; }
    
    [Column("IDPartner2")]
    public int? IDPartner2 { get; set; }
    
    [Column("ZakljucanDokument")]
    public bool? ZakljucanDokument { get; set; } = false;
    
    [Column("IDVrstaTroska")]
    public int? IDVrstaTroska { get; set; }
    
    [Column("IDPrikolica")]
    public int? IDPrikolica { get; set; }
    
    [Column("IDMesto1")]
    public int? IDMesto1 { get; set; }
    
    [Column("IDMesto2")]
    public int? IDMesto2 { get; set; }
    
    [Column("IDMerenje")]
    public int? IDMerenje { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<DocumentLineItem> LineItems { get; set; } = new List<DocumentLineItem>();
    public virtual ICollection<DocumentCost> DependentCosts { get; set; } = new List<DocumentCost>();
    public virtual ICollection<DocumentAdvanceVAT> AdvanceVATs { get; set; } = new List<DocumentAdvanceVAT>();
}
