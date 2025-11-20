namespace ERPAccounting.Domain.Lookups;

// ══════════════════════════════════════════════════
// SP 1: spPartnerComboStatusNabavka
// FIXED: Dodato 5 nedostajućih atributa
public record PartnerLookup(
    int IdPartner,
    string NazivPartnera,
    string? Mesto,
    string? Opis,              // Status description - NOVO!
    int IdStatus,
    int? IdNacinOporezivanjaNabavka,  // NOVO!
    short ObracunAkciza,       // NOVO!
    short ObracunPorez,        // NOVO!
    int? IdReferent,           // NOVO!
    string? Sifra
);

// ══════════════════════════════════════════════════
// SP 2: spOrganizacionaJedinicaCombo
// OK - bez izmena
public record OrgUnitLookup(
    int IdOrganizacionaJedinica,
    string Naziv,
    string? Mesto,
    string? Sifra
);

// ══════════════════════════════════════════════════
// SP 3: spNacinOporezivanjaComboNabavka
// FIXED: Dodato ObracunPorezPomocni
public record TaxationMethodLookup(
    int IdNacinOporezivanja,
    string Opis,
    short ObracunAkciza,
    short ObracunPorez,
    short ObracunPorezPomocni  // NOVO!
);

// ══════════════════════════════════════════════════
// SP 4: spReferentCombo
// OK - bez izmena
public record ReferentLookup(
    int IdRadnik,
    string ImeRadnika,
    string? SifraRadnika
);

// ══════════════════════════════════════════════════
// SP 5: spDokumentNDCombo
// OK - bez izmena
public record DocumentNDLookup(
    int IdDokument,
    string BrojDokumenta,
    DateTime Datum,
    string NazivPartnera
);

// ══════════════════════════════════════════════════
// SP 6: spPoreskaStopaCombo
// FIXED: Uklonjen ProcenatPDV (ne postoji u SP!)
public record TaxRateLookup(
    string IdPoreskaStopa,
    string Naziv
);

// ══════════════════════════════════════════════════
// SP 7: spArtikalComboUlaz
// FIXED: Dodato 7 nedostajućih atributa, promenjen NabavnaCena u OtkupnaCena
public record ArticleLookup(
    int IdArtikal,
    string SifraArtikal,
    string NazivArtikla,
    string? JedinicaMere,
    string? IdPoreskaStopa,    // NOVO!
    decimal ProcenatPoreza,    // NOVO!
    decimal Akciza,            // NOVO!
    decimal KoeficijentKolicine,  // NOVO!
    bool ImaLot,               // NOVO!
    decimal? OtkupnaCena,      // FIXED: bio NabavnaCena
    bool PoljoprivredniProizvod  // NOVO!
);

// ══════════════════════════════════════════════════
// SP 8: spDokumentTroskoviLista
// FIXED: Potpuno nova struktura prema stvarnom SP izlazu
public record DocumentCostLookup(
    int IdDokumentTroskovi,
    int? IdDokumentTroskoviStavka,
    string ListaZavisnihTroskova,
    decimal Osnovica,
    decimal Pdv
);

// ══════════════════════════════════════════════════
// SP 9: spUlazniRacuniIzvedeniTroskoviCombo
// FIXED: Dodato 3 nedostajuća atributa
public record CostTypeLookup(
    int IdUlazniRacuniIzvedeni,
    string Naziv,
    string? Opis,
    string? NazivSpecifikacije,  // NOVO!
    short ObracunPorez,          // NOVO!
    int IdUlazniRacuniOsnovni    // NOVO!
);

// ══════════════════════════════════════════════════
// SP 10: spNacinDeljenjaTroskovaCombo
// FIXED: Ispravljen naziv kolone
public record CostDistributionMethodLookup
{
    public int IdNacinDeljenjaTroskova { get; set; }  // FIXED: bio samo Id
    public string Naziv { get; set; } = string.Empty;
    public string OpisNacina { get; set; } = string.Empty;  // FIXED: bio samo Opis
}

// ══════════════════════════════════════════════════
// SP 11: spDokumentTroskoviArtikliCOMBO
// FIXED: Uklonjena Kolicina (ne postoji u SP!)
public record CostArticleLookup(
    int IdStavkaDokumenta,
    string SifraArtikal,
    string NazivArtikla
);
