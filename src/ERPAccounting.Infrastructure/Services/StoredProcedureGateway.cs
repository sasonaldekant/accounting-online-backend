using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.Services.Contracts;
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

    public async Task<List<PartnerComboDto>> GetPartnerComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<PartnerComboDto>("EXEC spPartnerComboStatusNabavka")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spPartnerComboStatusNabavka", ex);
        }
    }

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

    public async Task<List<TaxationMethodComboDto>> GetTaxationMethodsComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<TaxationMethodComboDto>("EXEC spNacinOporezivanjaComboNabavka")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spNacinOporezivanjaComboNabavka", ex);
        }
    }

    public async Task<List<ReferentComboDto>> GetReferentsComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<ReferentComboDto>("EXEC spReferentCombo")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spReferentCombo", ex);
        }
    }

    public async Task<List<DocumentNDComboDto>> GetDocumentNDComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<DocumentNDComboDto>("EXEC spDokumentNDCombo")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spDokumentNDCombo", ex);
        }
    }

    public async Task<List<TaxRateComboDto>> GetTaxRatesComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<TaxRateComboDto>("EXEC spPoreskaStopaCombo")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spPoreskaStopaCombo", ex);
        }
    }

    public async Task<List<ArticleComboDto>> GetArticlesComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<ArticleComboDto>("EXEC spArtikalComboUlaz")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spArtikalComboUlaz", ex);
        }
    }

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

    public async Task<List<CostTypeComboDto>> GetCostTypesComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<CostTypeComboDto>("EXEC spUlazniRacuniIzvedeniTroskoviCombo")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spUlazniRacuniIzvedeniTroskoviCombo", ex);
        }
    }

    public async Task<List<CostDistributionMethodComboDto>> GetCostDistributionMethodsComboAsync()
    {
        try
        {
            return await _context.Database
                .SqlQueryRaw<CostDistributionMethodComboDto>("EXEC spNacinDeljenjaTroskovaCombo")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Greška pri izvršavanju spNacinDeljenjaTroskovaCombo", ex);
        }
    }

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
