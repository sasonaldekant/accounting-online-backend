using ERPAccounting.Domain.Abstractions.Gateways;
using ERPAccounting.Domain.Lookups;
using ERPAccounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPAccounting.Infrastructure.Services;

/// <summary>
/// EF Core based implementation that executes the stored procedures used for lookup data.
/// </summary>
public class StoredProcedureGateway : IStoredProcedureGateway
{
    private readonly AppDbContext _context;

    public StoredProcedureGateway(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PartnerLookup>> GetPartnerComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<PartnerLookup>("EXEC spPartnerComboStatusNabavka")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spPartnerComboStatusNabavka", ex);
        }
    }

    public async Task<List<OrgUnitLookup>> GetOrgUnitsComboAsync(string docTypeId)
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<OrgUnitLookup>(
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

    public async Task<List<TaxationMethodLookup>> GetTaxationMethodsComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<TaxationMethodLookup>("EXEC spNacinOporezivanjaComboNabavka")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spNacinOporezivanjaComboNabavka", ex);
        }
    }

    public async Task<List<ReferentLookup>> GetReferentsComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<ReferentLookup>("EXEC spReferentCombo")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spReferentCombo", ex);
        }
    }

    public async Task<List<DocumentNDLookup>> GetDocumentNDComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<DocumentNDLookup>("EXEC spDokumentNDCombo")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spDokumentNDCombo", ex);
        }
    }

    public async Task<List<TaxRateLookup>> GetTaxRatesComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<TaxRateLookup>("EXEC spPoreskaStopaCombo")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spPoreskaStopaCombo", ex);
        }
    }

    public async Task<List<ArticleLookup>> GetArticlesComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<ArticleLookup>(
                    @"DECLARE @result TABLE (
                            IDArtikal INT,
                            SIFRA NVARCHAR(100),
                            [NAZIV ARTIKLA] NVARCHAR(255),
                            JM VARCHAR(6),
                            IDPoreskaStopa CHAR(2),
                            ProcenatPoreza FLOAT,
                            Akciza DECIMAL(19,4),
                            KoeficijentKolicine DECIMAL(19,4),
                            ImaLot BIT,
                            OtkupnaCena DECIMAL(19,4) NULL,
                            PoljoprivredniProizvod BIT
                        );

                        INSERT INTO @result
                        EXEC spArtikalComboUlaz;

                        SELECT IDArtikal,
                               SIFRA,
                               [NAZIV ARTIKLA],
                               JM,
                               IDPoreskaStopa,
                               ProcenatPoreza,
                               Akciza,
                               KoeficijentKolicine,
                               ImaLot,
                               OtkupnaCena,
                               PoljoprivredniProizvod
                        FROM @result;")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spArtikalComboUlaz", ex);
        }
    }

    public async Task<List<DocumentCostLookup>> GetDocumentCostsListAsync(int documentId)
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<DocumentCostLookup>(
                    @"DECLARE @result TABLE(
                            IDDokumentTroskovi INT,
                            IDDokumentTroskoviStavka INT NULL,
                            ListaTroskova NVARCHAR(MAX),
                            Osnovica DECIMAL(19,4) NULL,
                            Pdv DECIMAL(19,4) NULL
                        );

                        INSERT INTO @result
                        EXEC spDokumentTroskoviLista @IDDokument = {0};

                        SELECT IDDokumentTroskovi,
                               IDDokumentTroskoviStavka,
                               ListaTroskova,
                               Osnovica,
                               Pdv
                        FROM @result;",
                    documentId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spDokumentTroskoviLista", ex);
        }
    }

    public async Task<List<CostTypeLookup>> GetCostTypesComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<CostTypeLookup>("EXEC spUlazniRacuniIzvedeniTroskoviCombo")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spUlazniRacuniIzvedeniTroskoviCombo", ex);
        }
    }

    public async Task<List<CostDistributionMethodLookup>> GetCostDistributionMethodsComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<CostDistributionMethodLookup>("EXEC spNacinDeljenjaTroskovaCombo")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spNacinDeljenjaTroskovaCombo", ex);
        }
    }

    public async Task<List<CostArticleLookup>> GetCostArticlesComboAsync(int documentId)
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<CostArticleLookup>(
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

    // ══════════════════════════════════════════════════════════════
    // 🆕 NEW METHODS - Server-Side Search using Raw SQL
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Search partners using raw SQL query that replicates spPartnerComboStatusNabavka logic.
    /// Efficiently queries tblPartner with JOIN to tblStatus and tblMesto, applying search filter and TOP N limit.
    /// </summary>
    /// <param name="searchTerm">Search term to match against SifraPartner or NazivPartnera</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns>List of PartnerLookup matching the search criteria</returns>
    public async Task<List<PartnerLookup>> SearchPartnersAsync(string searchTerm, int limit)
    {
        try
        {
            var normalizedTerm = $"%{searchTerm.Trim()}%";

            // Replicates spPartnerComboStatusNabavka with search filter
            var results = await _context.Database
                .SqlQueryRaw<PartnerLookup>(
                    @"SELECT TOP ({1})
                        p.NazivPartnera AS [NAZIV PARTNERA],
                        m.NazivMesta AS MESTO,
                        p.IDPartner,
                        s.Opis,
                        p.IDStatus,
                        s.IDNacinOporezivanjaNabavka,
                        s.ObracunAkciza,
                        s.ObracunPorez,
                        p.IDReferent,
                        p.SifraPartner AS [ŠIFRA]
                    FROM dbo.tblPartner p
                    INNER JOIN dbo.tblStatus s ON p.IDStatus = s.IDStatus
                    LEFT OUTER JOIN dbo.tblMesto m ON p.IDMesto = m.IDMesto
                    WHERE p.SifraPartner LIKE {0}
                       OR p.NazivPartnera LIKE {0}
                    ORDER BY p.NazivPartnera",
                    normalizedTerm,
                    limit)
                .ToListAsync();

            return results;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri pretrazi partnera", ex);
        }
    }

    /// <summary>
    /// Search articles using raw SQL query that replicates spArtikalComboUlaz logic.
    /// FIXED: Cast Akciza and KoeficijentKolicine to DECIMAL(19,4) to match DTO expectations.
    /// Efficiently queries tblArtikal with JOIN to tblPoreskaStopa, applying search filter and TOP N limit.
    /// </summary>
    /// <param name="searchTerm">Search term to match against SifraArtikal or NazivArtikla</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns>List of ArticleLookup matching the search criteria</returns>
    public async Task<List<ArticleLookup>> SearchArticlesAsync(string searchTerm, int limit)
    {
        try
        {
            var normalizedTerm = $"%{searchTerm.Trim()}%";

            // Replicates spArtikalComboUlaz with search filter
            // FIXED: CAST Akciza, KoeficijentKolicine, OtkupnaCena to DECIMAL(19,4) to match ArticleLookup DTO
            var results = await _context.Database
                .SqlQueryRaw<ArticleLookup>(
                    @"SELECT TOP ({1})
                        a.IDArtikal,
                        a.SifraArtikal AS SIFRA,
                        a.NazivArtikla AS [NAZIV ARTIKLA],
                        a.IDJedinicaMere AS JM,
                        a.IDPoreskaStopa,
                        ps.ProcenatPoreza,
                        CAST(a.Akciza AS DECIMAL(19,4)) AS Akciza,
                        CAST(a.KoeficijentKolicine AS DECIMAL(19,4)) AS KoeficijentKolicine,
                        a.ImaLot,
                        CAST(a.OtkupnaCena AS DECIMAL(19,4)) AS OtkupnaCena,
                        a.PoljoprivredniProizvod
                    FROM dbo.tblArtikal a
                    INNER JOIN dbo.tblPoreskaStopa ps ON a.IDPoreskaStopa = ps.IDPoreskaStopa
                    WHERE a.SifraArtikal LIKE {0}
                       OR a.NazivArtikla LIKE {0}
                    ORDER BY a.SifraSort",
                    normalizedTerm,
                    limit)
                .ToListAsync();

            return results;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri pretrazi artikala", ex);
        }
    }
}
