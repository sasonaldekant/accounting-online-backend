using ERPAccounting.Application.DTOs;
using ERPAccounting.Domain.Abstractions.Gateways;
using ERPAccounting.Domain.Lookups;
using System.Linq;

namespace ERPAccounting.Application.Services
{
    /// <summary>
    /// Servis koji orkestrira sve lookup pozive ka bazi preko gateway-a
    /// kako bi se zadržala čista aplikaciona logika bez EF Core zavisnosti.
    /// </summary>
    public class StoredProcedureService : IStoredProcedureService
    {
        private readonly IStoredProcedureGateway _gateway;

        public StoredProcedureService(IStoredProcedureGateway gateway)
        {
            _gateway = gateway;
        }

        public async Task<List<PartnerComboDto>> GetPartnerComboAsync()
        {
            var partners = await _gateway.GetPartnerComboAsync();
            return partners.Select(MapToPartnerDto).ToList();
        }

        public async Task<List<OrgUnitComboDto>> GetOrgUnitsComboAsync(string docTypeId)
        {
            var orgUnits = await _gateway.GetOrgUnitsComboAsync(docTypeId);
            return orgUnits.Select(MapToOrgUnitDto).ToList();
        }

        public async Task<List<TaxationMethodComboDto>> GetTaxationMethodsComboAsync()
        {
            var taxationMethods = await _gateway.GetTaxationMethodsComboAsync();
            return taxationMethods.Select(MapToTaxationMethodDto).ToList();
        }

        public async Task<List<ReferentComboDto>> GetReferentsComboAsync()
        {
            var referents = await _gateway.GetReferentsComboAsync();
            return referents.Select(MapToReferentDto).ToList();
        }

        public async Task<List<DocumentNDComboDto>> GetDocumentNDComboAsync()
        {
            var documents = await _gateway.GetDocumentNDComboAsync();
            return documents.Select(MapToDocumentNDDto).ToList();
        }

        public async Task<List<TaxRateComboDto>> GetTaxRatesComboAsync()
        {
            var taxRates = await _gateway.GetTaxRatesComboAsync();
            return taxRates.Select(MapToTaxRateDto).ToList();
        }

        public async Task<List<ArticleComboDto>> GetArticlesComboAsync()
        {
            var articles = await _gateway.GetArticlesComboAsync();
            return articles.Select(MapToArticleDto).ToList();
        }

        public async Task<List<DocumentCostsListDto>> GetDocumentCostsListAsync(int documentId)
        {
            var costs = await _gateway.GetDocumentCostsListAsync(documentId);
            return costs.Select(MapToDocumentCostDto).ToList();
        }

        public async Task<List<CostTypeComboDto>> GetCostTypesComboAsync()
        {
            var costTypes = await _gateway.GetCostTypesComboAsync();
            return costTypes.Select(MapToCostTypeDto).ToList();
        }

        public async Task<List<CostDistributionMethodComboDto>> GetCostDistributionMethodsComboAsync()
        {
            var methods = await _gateway.GetCostDistributionMethodsComboAsync();
            return methods.Select(MapToCostDistributionMethodDto).ToList();
        }

        public async Task<List<CostArticleComboDto>> GetCostArticlesComboAsync(int documentId)
        {
            var articles = await _gateway.GetCostArticlesComboAsync(documentId);
            return articles.Select(MapToCostArticleDto).ToList();
        }

        private static PartnerComboDto MapToPartnerDto(PartnerLookup source) => new(
            source.IdPartner,
            source.NazivPartnera,
            source.Mesto,
            source.Sifra,
            source.Status);

        private static OrgUnitComboDto MapToOrgUnitDto(OrgUnitLookup source) => new(
            source.IdOrganizacionaJedinica,
            source.Naziv,
            source.Mesto,
            source.Sifra);

        private static TaxationMethodComboDto MapToTaxationMethodDto(TaxationMethodLookup source) => new(
            source.IdNacinOporezivanja,
            source.Opis,
            source.ObracunAkciza,
            source.ObracunPorez);

        private static ReferentComboDto MapToReferentDto(ReferentLookup source) => new(
            source.IdRadnik,
            source.ImeRadnika,
            source.SifraRadnika);

        private static DocumentNDComboDto MapToDocumentNDDto(DocumentNDLookup source) => new(
            source.IdDokument,
            source.BrojDokumenta,
            source.Datum,
            source.NazivPartnera);

        private static TaxRateComboDto MapToTaxRateDto(TaxRateLookup source) => new(
            source.IdPoreskaStopa,
            source.Naziv,
            source.ProcenatPDV);

        private static ArticleComboDto MapToArticleDto(ArticleLookup source) => new(
            source.IdArtikal,
            source.SifraArtikal,
            source.NazivArtikla,
            source.JedinicaMere,
            source.NabavnaCena);

        private static DocumentCostsListDto MapToDocumentCostDto(DocumentCostLookup source) => new(
            source.IdDokumentTroskovi,
            source.IdDokument,
            source.IdPartner,
            source.IdVrstaDokumenta,
            source.BrojDokumenta,
            source.DatumDPO,
            source.Opis);

        private static CostTypeComboDto MapToCostTypeDto(CostTypeLookup source) => new(
            source.IdUlazniRacuniIzvedeni,
            source.Naziv,
            source.Opis);

        private static CostDistributionMethodComboDto MapToCostDistributionMethodDto(CostDistributionMethodLookup source)
            => new()
            {
                Id = source.Id,
                Naziv = source.Naziv,
                Opis = source.Opis
            };

        private static CostArticleComboDto MapToCostArticleDto(CostArticleLookup source) => new(
            source.IdStavkaDokumenta,
            source.SifraArtikal,
            source.NazivArtikla,
            source.Kolicina);
    }
}
