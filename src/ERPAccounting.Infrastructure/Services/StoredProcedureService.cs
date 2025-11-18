using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.Services;
using ERPAccounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPAccounting.Infrastructure.Services
{
    /// <summary>
    /// Servis za sve 11 Stored Procedures koji vraćaju combo podatke
    /// OBAVEZNO: Sve SP-ove moraju biti dostupne u bazi!
    /// </summary>
    public class StoredProcedureService : ILookupService, IStoredProcedureService
    {
        private readonly AppDbContext _context;
        private readonly CacheService _cacheService;
        private static readonly TimeSpan CostDistributionCacheDuration = TimeSpan.FromHours(24);
        private const string CostDistributionCacheKey = "lookups:cost-distribution-methods";

        public StoredProcedureService(AppDbContext context, CacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        // ══════════════════════════════════════════════════
        // SP 1: spPartnerComboStatusNabavka
        /// <summary>
        /// Vraća listu partnera za nabavku sa statusom
        /// Koristi se za combo box partnera pri kreiranju dokumenata
        /// </summary>
        public async Task<List<PartnerComboDto>> GetPartnerComboAsync()
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<PartnerComboDto>(
                        "EXEC spPartnerComboStatusNabavka")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Greška pri izvršavanju spPartnerComboStatusNabavka", ex);
            }
        }

        // ══════════════════════════════════════════════════
        // SP 2: spOrganizacionaJedinicaCombo
        /// <summary>
        /// Vraća listu organizacionih jedinica za određenu vrstu dokumenta
        /// @IDVrstaDokumenta: "UR" za unos, "OD" za otpremu, itd.
        /// </summary>
        public async Task<List<OrgUnitComboDto>> GetOrgUnitsComboAsync(string docTypeId)
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<OrgUnitComboDto>(
                        "EXEC spOrganizacionaJedinicaCombo @IDVrstaDokumenta = {0}",
                        docTypeId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Greška pri izvršavanju spOrganizacionaJedinicaCombo", ex);
            }
        }

        // ══════════════════════════════════════════════════
        // SP 3: spNacinOporezivanjaComboNabavka
        /// <summary>
        /// Vraća listu načina oporezivanja za nabavne dokumente
        /// Koristi se za izbor metode oporezivanja
        /// </summary>
        public async Task<List<TaxationMethodComboDto>> GetTaxationMethodsComboAsync()
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<TaxationMethodComboDto>(
                        "EXEC spNacinOporezivanjaComboNabavka")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Greška pri izvršavanju spNacinOporezivanjaComboNabavka", ex);
            }
        }

        // ══════════════════════════════════════════════════
        // SP 4: spReferentCombo
        /// <summary>
        /// Vraća listu referenata (zaposlenih)
        /// Koristi se za izbor referenta za dokument
        /// </summary>
        public async Task<List<ReferentComboDto>> GetReferentsComboAsync()
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<ReferentComboDto>(
                        "EXEC spReferentCombo")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Greška pri izvršavanju spReferentCombo", ex);
            }
        }

        // ══════════════════════════════════════════════════
        // SP 5: spDokumentNDCombo
        /// <summary>
        /// Vraća listu ND dokumenata kao referentnih dokumenata
        /// Koristi se za linkovanje sa referentnim dokumentom
        /// </summary>
        public async Task<List<DocumentNDComboDto>> GetDocumentNDComboAsync()
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<DocumentNDComboDto>(
                        "EXEC spDokumentNDCombo")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Greška pri izvršavanju spDokumentNDCombo", ex);
            }
        }

        // ══════════════════════════════════════════════════
        // SP 6: spPoreskaStopaCombo
        /// <summary>
        /// Vraća listu poreskih stopa
        /// Koristi se za izbor PDV stope pri kreiranju stavki
        /// </summary>
        public async Task<List<TaxRateComboDto>> GetTaxRatesComboAsync()
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<TaxRateComboDto>(
                        "EXEC spPoreskaStopaCombo")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Greška pri izvršavanju spPoreskaStopaCombo", ex);
            }
        }

        // ══════════════════════════════════════════════════
        // SP 7: spArtikalComboUlaz
        /// <summary>
        /// Vraća listu artikala za unos/nabavne dokumente
        /// Koristi se za autocomplete/combo pri izboru artikla
        /// </summary>
        public async Task<List<ArticleComboDto>> GetArticlesComboAsync()
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<ArticleComboDto>(
                        "EXEC spArtikalComboUlaz")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Greška pri izvršavanju spArtikalComboUlaz", ex);
            }
        }

        // ══════════════════════════════════════════════════
        // SP 8: spDokumentTroskoviLista
        /// <summary>
        /// Vraća listu troškova za određeni dokument
        /// @IDDokument: ID dokumenta
        /// </summary>
        public async Task<List<DocumentCostsListDto>> GetDocumentCostsListAsync(int documentId)
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<DocumentCostsListDto>(
                        "EXEC spDokumentTroskoviLista @IDDokument = {0}",
                        documentId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Greška pri izvršavanju spDokumentTroskoviLista", ex);
            }
        }

        // ══════════════════════════════════════════════════
        // SP 9: spUlazniRacuniIzvedeniTroskoviCombo
        /// <summary>
        /// Vraća listu vrsta troškova za obradu troškova
        /// Koristi se za izbor vrste troška pri kreiranju troška
        /// </summary>
        public async Task<List<CostTypeComboDto>> GetCostTypesComboAsync()
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<CostTypeComboDto>(
                        "EXEC spUlazniRacuniIzvedeniTroskoviCombo")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Greška pri izvršavanju spUlazniRacuniIzvedeniTroskoviCombo", ex);
            }
        }

        // ══════════════════════════════════════════════════
        // SP 10: spNacinDeljenjaTroskovaCombo
        /// <summary>
        /// Vraća listu načina deljenja troškova: 1=Po količini, 2=Po vrednosti, 3=Ručno
        /// Hardcoded jer se ne menja
        /// </summary>
        public async Task<List<CostDistributionMethodComboDto>> GetCostDistributionMethodsComboAsync()
        {
            return await _cacheService.GetOrCreateAsync(
                CostDistributionCacheKey,
                () => Task.FromResult(new List<CostDistributionMethodComboDto>
                {
                    new() { Id = 1, Naziv = "Po količini", Opis = "Raspodela proporcionalno količini" },
                    new() { Id = 2, Naziv = "Po vrednosti", Opis = "Raspodela proporcionalno vrednosti" },
                    new() { Id = 3, Naziv = "Ručno", Opis = "Ručno unošenje raspodele" }
                }),
                CostDistributionCacheDuration);
        }

        // ══════════════════════════════════════════════════
        // SP 11: spDokumentTroskoviArtikliCOMBO
        /// <summary>
        /// Vraća listu artikala iz stavki određenog dokumenta za raspoređivanje troškova
        /// @IDDokument: ID dokumenta
        /// </summary>
        public async Task<List<CostArticleComboDto>> GetCostArticlesComboAsync(int documentId)
        {
            try
            {
                return await _context.Database
                    .SqlQueryRaw<CostArticleComboDto>(
                        "EXEC spDokumentTroskoviArtikliCOMBO @IDDokument = {0}",
                        documentId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Greška pri izvršavanju spDokumentTroskoviArtikliCOMBO", ex);
            }
        }
    }
}
