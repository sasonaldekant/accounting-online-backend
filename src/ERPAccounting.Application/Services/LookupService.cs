using ERPAccounting.Application.DTOs;
using ERPAccounting.Domain.Abstractions.Gateways;
using ERPAccounting.Domain.Lookups;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERPAccounting.Application.Services;

/// <summary>
/// High-level lookup orchestrator consumed by API controllers.
/// Keeps controller logic thin by retrieving data via the stored-procedure gateway and mapping results to DTOs.
/// </summary>
public class LookupService : ILookupService
{
    private const string DefaultDocumentTypeId = "UR";
    private readonly IStoredProcedureGateway _storedProcedureGateway;

    public LookupService(IStoredProcedureGateway storedProcedureGateway)
    {
        _storedProcedureGateway = storedProcedureGateway;
    }

    // ═══════════════════════════════════════════════════════════════
    // ORIGINAL METHODS (Stored Procedures)
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<PartnerComboDto>> GetPartnerComboAsync()
    {
        var partners = await _storedProcedureGateway.GetPartnerComboAsync();
        return partners.Select(MapToPartnerDto).ToList();
    }

    public async Task<List<OrgUnitComboDto>> GetOrgUnitsComboAsync(string? docTypeId = null)
    {
        var effectiveDocTypeId = string.IsNullOrWhiteSpace(docTypeId)
            ? DefaultDocumentTypeId
            : docTypeId!;

        var orgUnits = await _storedProcedureGateway.GetOrgUnitsComboAsync(effectiveDocTypeId);
        return orgUnits.Select(MapToOrgUnitDto).ToList();
    }

    public async Task<List<TaxationMethodComboDto>> GetTaxationMethodsComboAsync()
    {
        var taxationMethods = await _storedProcedureGateway.GetTaxationMethodsComboAsync();
        return taxationMethods.Select(MapToTaxationMethodDto).ToList();
    }

    public async Task<List<ReferentComboDto>> GetReferentsComboAsync()
    {
        var referents = await _storedProcedureGateway.GetReferentsComboAsync();
        return [.. referents.Select(MapToReferentDto)];
    }

    public async Task<List<DocumentNDComboDto>> GetDocumentNDComboAsync()
    {
        var documents = await _storedProcedureGateway.GetDocumentNDComboAsync();
        return [.. documents.Select(MapToDocumentNDDto)];
    }

    public async Task<List<TaxRateComboDto>> GetTaxRatesComboAsync()
    {
        var taxRates = await _storedProcedureGateway.GetTaxRatesComboAsync();
        return [.. taxRates.Select(MapToTaxRateDto)];
    }

    public async Task<List<ArticleComboDto>> GetArticlesComboAsync()
    {
        var articles = await _storedProcedureGateway.GetArticlesComboAsync();
        return [.. articles.Select(MapToArticleDto)];
    }

    public async Task<List<DocumentCostsListDto>> GetDocumentCostsListAsync(int documentId)
    {
        var costs = await _storedProcedureGateway.GetDocumentCostsListAsync(documentId);
        return [.. costs.Select(MapToDocumentCostDto)];
    }

    public async Task<List<CostTypeComboDto>> GetCostTypesComboAsync()
    {
        var costTypes = await _storedProcedureGateway.GetCostTypesComboAsync();
        return [.. costTypes.Select(MapToCostTypeDto)];
    }

    public async Task<List<CostDistributionMethodComboDto>> GetCostDistributionMethodsComboAsync()
    {
        var methods = await _storedProcedureGateway.GetCostDistributionMethodsComboAsync();
        return [.. methods.Select(MapToCostDistributionMethodDto)];
    }

    public async Task<List<CostArticleComboDto>> GetCostArticlesComboAsync(int documentId)
    {
        var articles = await _storedProcedureGateway.GetCostArticlesComboAsync(documentId);
        return [.. articles.Select(MapToCostArticleDto)];
    }

    // ═══════════════════════════════════════════════════════════════
    // 🆕 NEW METHODS - Server-Side Search
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Pretraga partnera po šifri ili nazivu (server-side filtering).
    /// </summary>
    public async Task<List<PartnerComboDto>> SearchPartnersAsync(string searchTerm, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            return new List<PartnerComboDto>();
        }

        var partners = await _storedProcedureGateway.SearchPartnersAsync(searchTerm, limit);
        return partners.Select(MapToPartnerDto).ToList();
    }

    /// <summary>
    /// Pretraga artikala po šifri ili nazivu (server-side filtering).
    /// </summary>
    public async Task<List<ArticleComboDto>> SearchArticlesAsync(string searchTerm, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            return new List<ArticleComboDto>();
        }

        var articles = await _storedProcedureGateway.SearchArticlesAsync(searchTerm, limit);
        return articles.Select(MapToArticleDto).ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // MAPPING FUNCTIONS
    // ═══════════════════════════════════════════════════════════════

    private static PartnerComboDto MapToPartnerDto(PartnerLookup source) => new(
        source.IdPartner,
        source.NazivPartnera,
        source.Mesto,
        source.Opis,
        source.IdStatus,
        source.IdNacinOporezivanjaNabavka,
        source.ObracunAkciza,
        source.ObracunPorez,
        source.IdReferent,
        source.SifraPartner
    );

    private static OrgUnitComboDto MapToOrgUnitDto(OrgUnitLookup source) => new(
        source.IdOrganizacionaJedinica,
        source.Naziv,
        source.Mesto,
        source.Sifra
    );

    private static TaxationMethodComboDto MapToTaxationMethodDto(TaxationMethodLookup source) => new(
        source.IdNacinOporezivanja,
        source.Opis,
        source.ObracunAkciza,
        source.ObracunPorez,
        source.ObracunPorezPomocni
    );

    private static ReferentComboDto MapToReferentDto(ReferentLookup source) => new(
        source.IdRadnik,
        source.ImePrezime,  // Matches SQL alias "IME I PREZIME"
        source.SifraRadnika
    );

    private static DocumentNDComboDto MapToDocumentNDDto(DocumentNDLookup source) => new(
        source.IdDokument,
        source.BrojDokumenta,
        source.Datum,
        source.NazivPartnera
    );

    // FIX: Removed ProcenatPoreza - not available from spPoreskaStopaCombo
    private static TaxRateComboDto MapToTaxRateDto(TaxRateLookup source) => new(
        source.IdPoreskaStopa,
        source.Naziv
        // ProcenatPoreza - NOT available from this SP
    );

    private static ArticleComboDto MapToArticleDto(ArticleLookup source) => new(
        source.IdArtikal,
        source.SifraArtikal,
        source.NazivArtikla,
        source.JedinicaMere,
        source.IdPoreskaStopa,
        source.ProcenatPoreza,  // Available from spArtikalComboUlaz
        source.Akciza,
        source.KoeficijentKolicine,
        source.ImaLot,
        source.OtkupnaCena,
        source.PoljoprivredniProizvod
    );

    private static DocumentCostsListDto MapToDocumentCostDto(DocumentCostLookup source)
    {
        var osnovica = source.Osnovica;
        var pdv = source.Pdv;

        return new(
            source.IdDokumentTroskovi,
            source.IdDokumentTroskoviStavka,
            source.ListaTroskova,
            osnovica,
            pdv
        );
    }

    private static CostTypeComboDto MapToCostTypeDto(CostTypeLookup source) => new(
        source.IdUlazniRacuniIzvedeni,
        source.Naziv,
        source.Opis,
        source.NazivSpecifikacije,
        source.ObracunPorez,
        source.IdUlazniRacuniOsnovni
    );

    private static CostDistributionMethodComboDto MapToCostDistributionMethodDto(CostDistributionMethodLookup source)
        => new()
        {
            IdNacinDeljenjaTroskova = source.IdNacinDeljenjaTroskova,
            Naziv = source.Naziv,
            OpisNacina = source.OpisNacina
        };

    private static CostArticleComboDto MapToCostArticleDto(CostArticleLookup source) => new(
        source.IdStavkaDokumenta,
        source.SifraArtikal,
        source.NazivArtikla
    );
};