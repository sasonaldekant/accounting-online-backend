namespace ERPAccounting.Domain.Lookups;

// ══════════════════════════════════════════════════
// SP 1: spPartnerComboStatusNabavka
public record PartnerLookup(
    int IdPartner,
    string NazivPartnera,
    string? Mesto,
    string? Sifra,
    int Status
);

// ══════════════════════════════════════════════════
// SP 2: spOrganizacionaJedinicaCombo
public record OrgUnitLookup(
    int IdOrganizacionaJedinica,
    string Naziv,
    string? Mesto,
    string? Sifra
);

// ══════════════════════════════════════════════════
// SP 3: spNacinOporezivanjaComboNabavka
public record TaxationMethodLookup(
    int IdNacinOporezivanja,
    string Opis,
    short ObracunAkciza,
    short ObracunPorez
);

// ══════════════════════════════════════════════════
// SP 4: spReferentCombo
public record ReferentLookup(
    int IdRadnik,
    string ImeRadnika,
    string? SifraRadnika
);

// ══════════════════════════════════════════════════
// SP 5: spDokumentNDCombo
public record DocumentNDLookup(
    int IdDokument,
    string BrojDokumenta,
    DateTime Datum,
    string NazivPartnera
);

// ══════════════════════════════════════════════════
// SP 6: spPoreskaStopaCombo
public record TaxRateLookup(
    string IdPoreskaStopa,
    string Naziv,
    decimal ProcenatPDV
);

// ══════════════════════════════════════════════════
// SP 7: spArtikalComboUlaz
public record ArticleLookup(
    int IdArtikal,
    string SifraArtikal,
    string NazivArtikla,
    string? JedinicaMere,
    decimal? NabavnaCena
);

// ══════════════════════════════════════════════════
// SP 8: spDokumentTroskoviLista
public record DocumentCostLookup(
    int IdDokumentTroskovi,
    int IdDokument,
    int? IdPartner,
    string? IdVrstaDokumenta,
    string? BrojDokumenta,
    DateTime? DatumDPO,
    string? Opis
);

// ══════════════════════════════════════════════════
// SP 9: spUlazniRacuniIzvedeniTroskoviCombo
public record CostTypeLookup(
    int IdUlazniRacuniIzvedeni,
    string Naziv,
    string? Opis
);

// ══════════════════════════════════════════════════
// SP 10: spNacinDeljenjaTroskovaCombo
public record CostDistributionMethodLookup
{
    public int Id { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string Opis { get; set; } = string.Empty;
}

// ══════════════════════════════════════════════════
// SP 11: spDokumentTroskoviArtikliCOMBO
public record CostArticleLookup(
    int IdStavkaDokumenta,
    string SifraArtikal,
    string NazivArtikla,
    decimal Kolicina
);
