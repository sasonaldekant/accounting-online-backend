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
                .SqlQueryRaw<ArticleLookup>("EXEC spArtikalComboUlaz")
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
                            Osnovica DECIMAL(19,4),
                            Pdv DECIMAL(19,4)
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
}
