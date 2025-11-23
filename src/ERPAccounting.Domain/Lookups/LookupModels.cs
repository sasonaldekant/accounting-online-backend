using System.ComponentModel.DataAnnotations.Schema;

namespace ERPAccounting.Domain.Lookups;

// SP 1: spPartnerComboStatusNabavka
public record PartnerLookup(
    [property: Column("IDPartner")] int IdPartner,
    [property: Column("NAZIV PARTNERA")] string NazivPartnera,
    [property: Column("MESTO")] string? Mesto,
    [property: Column("Opis")] string? Opis,
    [property: Column("IDStatus")] int IdStatus,
    [property: Column("IDNacinOporezivanjaNabavka")] int? IdNacinOporezivanjaNabavka,
    [property: Column("ObracunAkciza")] short ObracunAkciza,
    [property: Column("ObracunPorez")] short ObracunPorez,
    [property: Column("IDReferent")] int? IdReferent,
    [property: Column("ŠIFRA")] string? SifraPartner
);

// SP 2: spOrganizacionaJedinicaCombo
public record OrgUnitLookup(
    [property: Column("IDOrganizacionaJedinica")] int IdOrganizacionaJedinica,
    [property: Column("NAZIV MAGACINA")] string Naziv,
    [property: Column("MESTO")] string? Mesto,
    [property: Column("SifraOrganizacionaJedinica")] string? Sifra
);

// SP 3: spNacinOporezivanjaComboNabavka
public record TaxationMethodLookup(
    [property: Column("IDNacinOporezivanja")] int IdNacinOporezivanja,
    [property: Column("Opis")] string Opis,
    [property: Column("ObracunAkciza")] short ObracunAkciza,
    [property: Column("ObracunPorez")] short ObracunPorez,
    [property: Column("ObracunPorezPomocni")] short ObracunPorezPomocni
);

// SP 4: spReferentCombo
public record ReferentLookup(
    [property: Column("IDRadnik")] int IdRadnik,
    [property: Column("IME I PREZIME")] string ImeRadnika,
    [property: Column("SifraRadnika")] string? SifraRadnika
);

// SP 5: spDokumentNDCombo
public record DocumentNDLookup(
    [property: Column("IDDokument")] int IdDokument,
    [property: Column("BrojDokumenta")] string BrojDokumenta,
    [property: Column("Datum")] DateTime Datum,
    [property: Column("NazivPartnera")] string NazivPartnera
);

// SP 6: spPoreskaStopaCombo
public record TaxRateLookup(
    [property: Column("IDPoreskaStopa")] string IdPoreskaStopa,
    [property: Column("Naziv")] string Naziv
);

// SP 7: spArtikalComboUlaz
public record ArticleLookup(
    [property: Column("IDArtikal")] int IdArtikal,
    [property: Column("SIFRA")] string SifraArtikal,
    [property: Column("NAZIV ARTIKLA")] string NazivArtikla,
    [property: Column("JM")] string? JedinicaMere,
    [property: Column("IDPoreskaStopa")] string? IdPoreskaStopa,
    [property: Column("ProcenatPoreza")] decimal ProcenatPoreza,
    [property: Column("Akciza")] decimal Akciza,
    [property: Column("KoeficijentKolicine")] decimal KoeficijentKolicine,
    [property: Column("ImaLot")] bool ImaLot,
    [property: Column("OtkupnaCena")] decimal? OtkupnaCena,
    [property: Column("PoljoprivredniProizvod")] bool PoljoprivredniProizvod
);

// SP 8: spDokumentTroskoviLista
public record DocumentCostLookup(
    [property: Column("IDDokumentTroskovi")] int IdDokumentTroskovi,
    [property: Column("IDDokumentTroskoviStavka")] int? IdDokumentTroskoviStavka,
    [property: Column("ListaTroskova")] string ListaTroskova,
    [property: Column("OSNOVICA")] decimal Osnovica,
    [property: Column("PDV")] decimal Pdv
);

// SP 9: spUlazniRacuniIzvedeniTroskoviCombo
// ObracunPorez is an int to match the stored procedure output and avoid InvalidCastException.
public record CostTypeLookup(
    [property: Column("IDUlazniRacuniIzvedeni")] int IdUlazniRacuniIzvedeni,
    [property: Column("Naziv")] string Naziv,
    [property: Column("Opis")] string? Opis,
    [property: Column("NazivSpecifikacije")] string? NazivSpecifikacije,
    [property: Column("ObracunPorez")] int ObracunPorez,
    [property: Column("IDUlazniRacuniOsnovni")] int IdUlazniRacuniOsnovni
);

// SP 10: spNacinDeljenjaTroskovaCombo
public record CostDistributionMethodLookup
{
    [Column("IDNacinDeljenjaTroskova")]
    public short IdNacinDeljenjaTroskova { get; set; }

    [Column("Naziv")]
    public string Naziv { get; set; } = string.Empty;

    [Column("OpisNacina")]
    public string OpisNacina { get; set; } = string.Empty;
}

// SP 11: spDokumentTroskoviArtikliCOMBO
public record CostArticleLookup(
    [property: Column("IDStavkaDokumenta")] int IdStavkaDokumenta,
    [property: Column("SifraArtikal")] string SifraArtikal,
    [property: Column("NazivArtikla")] string NazivArtikla
);
