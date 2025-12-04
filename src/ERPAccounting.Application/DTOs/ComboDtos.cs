namespace ERPAccounting.Application.DTOs;

// SP 1: spPartnerComboStatusNabavka
public record PartnerComboDto(
    int IdPartner,
    string NazivPartnera,
    string? Mesto,
    string? Opis,
    int IdStatus,
    int? IdNacinOporezivanjaNabavka,
    short ObracunAkciza,
    short ObracunPorez,
    int? IdReferent,
    string? SifraPartner
);

// SP 2: spOrganizacionaJedinicaCombo
public record OrgUnitComboDto(
    int IdOrganizacionaJedinica,
    string Naziv,
    string? Mesto,
    string? Sifra
);

// SP 3: spNacinOporezivanjaComboNabavka
public record TaxationMethodComboDto(
    int IdNacinOporezivanja,
    string Opis,
    short ObracunAkciza,
    short ObracunPorez,
    short ObracunPorezPomocni
);

// SP 4: spReferentCombo
// Returns: IDRadnik, ImeRadnika as [IME I PREZIME], SifraRadnika
public record ReferentComboDto(
    int IdRadnik,
    string ImePrezime,  // Matches SQL alias "IME I PREZIME"
    string? SifraRadnika
);

// SP 5: spDokumentNDCombo
public record DocumentNDComboDto(
    int IdDokument,
    string BrojDokumenta,
    DateTime Datum,
    string NazivPartnera
);

// SP 6: spPoreskaStopaCombo
// Returns: IDPoreskaStopa, Naziv (ONLY 2 columns)
// NOTE: ProcenatPoreza is NOT available from this SP.
//       It's only available via spArtikalComboUlaz when fetching articles.
public record TaxRateComboDto(
    string IdPoreskaStopa,
    string Naziv
    // ProcenatPoreza - REMOVED: Not returned by spPoreskaStopaCombo
);

// SP 7: spArtikalComboUlaz
// This SP DOES include ProcenatPoreza via JOIN with tblPoreskaStopa
public record ArticleComboDto(
    int IdArtikal,
    string SifraArtikal,
    string NazivArtikla,
    string? JedinicaMere,
    string? IdPoreskaStopa,
    double ProcenatPoreza,  // Available HERE
    decimal Akciza,
    decimal KoeficijentKolicine,
    bool ImaLot,
    decimal? OtkupnaCena,
    bool PoljoprivredniProizvod
);

// SP 8: spDokumentTroskoviLista
public record DocumentCostsListDto(
    int IdDokumentTroskovi,
    int? IdDokumentTroskoviStavka,
    string ListaTroskova,
    decimal? Osnovica,
    decimal? Pdv
);

// SP 9: spUlazniRacuniIzvedeniTroskoviCombo
public record CostTypeComboDto(
    int IdUlazniRacuniIzvedeni,
    string Naziv,
    string? Opis,
    string? NazivSpecifikacije,
    int ObracunPorez,
    int IdUlazniRacuniOsnovni
);

// SP 10: spNacinDeljenjaTroskovaCombo
public record CostDistributionMethodComboDto
{
    public int IdNacinDeljenjaTroskova { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string OpisNacina { get; set; } = string.Empty;
}

// SP 11: spDokumentTroskoviArtikliCOMBO
public record CostArticleComboDto(
    int IdStavkaDokumenta,
    string SifraArtikal,
    string NazivArtikla
);