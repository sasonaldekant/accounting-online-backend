using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.Services;
using ERPAccounting.Common.Constants;
using ERPAccounting.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace ERPAccounting.API.Controllers
{
    /// <summary>
    /// Lookup Controller - Svi combo endpointi za popunjavanje dropdowns
    /// Koristi 11 Stored Procedures iz baze
    /// </summary>
    [ApiController]
    [Route(ApiRoutes.Lookups.Base)]
    [Authorize]
    public class LookupsController : ControllerBase
    {
        private readonly ILookupService _lookupService;
        private readonly ILogger<LookupsController> _logger;

        public LookupsController(
            ILookupService lookupService,
            ILogger<LookupsController> logger)
        {
            _lookupService = lookupService;
            _logger = logger;
        }

        [HttpGet(ApiRoutes.Lookups.Partners)]
        [ProducesResponseType(typeof(List<PartnerComboDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<PartnerComboDto>>> GetPartners()
            => ExecuteLookupAsync(async () =>
            {
                var result = await _lookupService.GetPartnerComboAsync();
                _logger.LogInformation("Partners loaded: {Count}", result.Count);
                return result;
            }, "partnera");

        [HttpGet(ApiRoutes.Lookups.OrganizationalUnits)]
        [ProducesResponseType(typeof(List<OrgUnitComboDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<OrgUnitComboDto>>> GetOrgUnits([FromQuery] string? docTypeId = null)
            => ExecuteLookupAsync(async () =>
            {
                var result = await _lookupService.GetOrgUnitsComboAsync(docTypeId);
                _logger.LogInformation("Organizational units loaded for {DocType}: {Count}", docTypeId, result.Count);
                return result;
            }, "organizacionih jedinica");

        [HttpGet(ApiRoutes.Lookups.TaxationMethods)]
        [ProducesResponseType(typeof(List<TaxationMethodComboDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<TaxationMethodComboDto>>> GetTaxationMethods()
            => ExecuteLookupAsync(_lookupService.GetTaxationMethodsComboAsync, "načina oporezivanja");

        [HttpGet(ApiRoutes.Lookups.Referents)]
        [ProducesResponseType(typeof(List<ReferentComboDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<ReferentComboDto>>> GetReferents()
            => ExecuteLookupAsync(_lookupService.GetReferentsComboAsync, "referenata");

        [HttpGet(ApiRoutes.Lookups.DocumentsNd)]
        [ProducesResponseType(typeof(List<DocumentNDComboDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<DocumentNDComboDto>>> GetDocumentsND()
            => ExecuteLookupAsync(_lookupService.GetDocumentNDComboAsync, "ND dokumenata");

        [HttpGet(ApiRoutes.Lookups.TaxRates)]
        [ProducesResponseType(typeof(List<TaxRateComboDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<TaxRateComboDto>>> GetTaxRates()
            => ExecuteLookupAsync(_lookupService.GetTaxRatesComboAsync, "poreskih stopa");

        [HttpGet(ApiRoutes.Lookups.Articles)]
        [ProducesResponseType(typeof(List<ArticleComboDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<ArticleComboDto>>> GetArticles()
            => ExecuteLookupAsync(_lookupService.GetArticlesComboAsync, "artikala");

        [HttpGet(ApiRoutes.Lookups.DocumentCosts)]
        [ProducesResponseType(typeof(List<DocumentCostsListDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<DocumentCostsListDto>>> GetDocumentCosts(int documentId)
            => ExecuteLookupAsync(() => _lookupService.GetDocumentCostsListAsync(documentId), "troškova");

        [HttpGet(ApiRoutes.Lookups.CostTypes)]
        [ProducesResponseType(typeof(List<CostTypeComboDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<CostTypeComboDto>>> GetCostTypes()
            => ExecuteLookupAsync(_lookupService.GetCostTypesComboAsync, "vrsta troškova");

        [HttpGet(ApiRoutes.Lookups.CostDistributionMethods)]
        [ProducesResponseType(typeof(List<CostDistributionMethodComboDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<CostDistributionMethodComboDto>>> GetCostDistributionMethods()
            => ExecuteLookupAsync(_lookupService.GetCostDistributionMethodsComboAsync, "načina deljenja troškova");

        [HttpGet(ApiRoutes.Lookups.CostArticles)]
        [ProducesResponseType(typeof(List<CostArticleComboDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<CostArticleComboDto>>> GetCostArticles(int documentId)
            => ExecuteLookupAsync(() => _lookupService.GetCostArticlesComboAsync(documentId), "artikala za troškove");

        private async Task<ActionResult<List<T>>> ExecuteLookupAsync<T>(Func<Task<List<T>>> action, string resourceName)
        {
            try
            {
                var result = await action();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading {Resource}", resourceName);
                throw CreateLookupException(resourceName, ex);
            }
        }

        private static DomainException CreateLookupException(string resourceName, Exception innerException)
        {
            var detail = string.Format(CultureInfo.InvariantCulture, ErrorMessages.LookupLoadFailed, resourceName);
            return new DomainException(
                HttpStatusCode.InternalServerError,
                ErrorMessages.LookupErrorTitle,
                detail,
                ErrorCodes.LookupFailed,
                innerException: innerException);
        }
    }
}
