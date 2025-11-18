using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.Services.Contracts;

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

        public Task<List<PartnerComboDto>> GetPartnerComboAsync()
            => _gateway.GetPartnerComboAsync();

        public Task<List<OrgUnitComboDto>> GetOrgUnitsComboAsync(string docTypeId)
            => _gateway.GetOrgUnitsComboAsync(docTypeId);

        public Task<List<TaxationMethodComboDto>> GetTaxationMethodsComboAsync()
            => _gateway.GetTaxationMethodsComboAsync();

        public Task<List<ReferentComboDto>> GetReferentsComboAsync()
            => _gateway.GetReferentsComboAsync();

        public Task<List<DocumentNDComboDto>> GetDocumentNDComboAsync()
            => _gateway.GetDocumentNDComboAsync();

        public Task<List<TaxRateComboDto>> GetTaxRatesComboAsync()
            => _gateway.GetTaxRatesComboAsync();

        public Task<List<ArticleComboDto>> GetArticlesComboAsync()
            => _gateway.GetArticlesComboAsync();

        public Task<List<DocumentCostsListDto>> GetDocumentCostsListAsync(int documentId)
            => _gateway.GetDocumentCostsListAsync(documentId);

        public Task<List<CostTypeComboDto>> GetCostTypesComboAsync()
            => _gateway.GetCostTypesComboAsync();

        public Task<List<CostDistributionMethodComboDto>> GetCostDistributionMethodsComboAsync()
            => Task.FromResult(new List<CostDistributionMethodComboDto>
            {
                new() { Id = 1, Naziv = "Po količini", Opis = "Raspodela proporcionalno količini" },
                new() { Id = 2, Naziv = "Po vrednosti", Opis = "Raspodela proporcionalno vrednosti" },
                new() { Id = 3, Naziv = "Ručno", Opis = "Ručno unošenje raspodele" }
            });

        public Task<List<CostArticleComboDto>> GetCostArticlesComboAsync(int documentId)
            => _gateway.GetCostArticlesComboAsync(documentId);
    }
}
