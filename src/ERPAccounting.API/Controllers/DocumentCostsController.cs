using System.Collections.Generic;
using System.Linq;
using ERPAccounting.Application.DTOs.Costs;
using ERPAccounting.Application.Services;
using ERPAccounting.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPAccounting.API.Controllers
{
    /// <summary>
    /// Upravljanje zavisnim troškovima i njihovim stavkama sa ETag podrškom
    /// </summary>
    [ApiController]
    [Route("api/v1/documents/{documentId:int}/costs")]
    [Authorize]
    public class DocumentCostsController : ControllerBase
    {
        private readonly IDocumentCostService _costService;
        private readonly ILogger<DocumentCostsController> _logger;

        public DocumentCostsController(IDocumentCostService costService, ILogger<DocumentCostsController> logger)
        {
            _costService = costService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<DocumentCostDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<DocumentCostDto>>> GetCosts(int documentId)
        {
            var costs = await _costService.GetCostsAsync(documentId);
            return Ok(costs);
        }

        [HttpGet("{costId:int}")]
        [ProducesResponseType(typeof(DocumentCostDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentCostDto>> GetCost(int documentId, int costId)
        {
            var cost = await _costService.GetCostByIdAsync(documentId, costId);
            if (cost == null)
            {
                return NotFound(new { message = "Trošak nije pronađen" });
            }

            Response.Headers["ETag"] = $"\"{cost.ETag}\"";
            return Ok(cost);
        }

        [HttpPost]
        [ProducesResponseType(typeof(DocumentCostDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentCostDto>> CreateCost(int documentId, [FromBody] CreateDocumentCostDto dto)
        {
            try
            {
                var created = await _costService.CreateCostAsync(documentId, dto);
                Response.Headers["ETag"] = $"\"{created.ETag}\"";
                return CreatedAtAction(nameof(GetCost), new { documentId, costId = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating cost for document {DocumentId}", documentId);
                return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Document not found while creating cost for document {DocumentId}", documentId);
                return NotFound(new { message = ex.Detail });
            }
        }

        [HttpPut("{costId:int}")]
        [ProducesResponseType(typeof(DocumentCostDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DocumentCostDto>> UpdateCost(int documentId, int costId, [FromBody] UpdateDocumentCostDto dto)
        {
            try
            {
                var expectedRowVersion = ExtractRowVersionFromIfMatch();
                if (expectedRowVersion == null)
                {
                    return BadRequest(new { message = "Missing or invalid If-Match header" });
                }

                var updated = await _costService.UpdateCostAsync(documentId, costId, expectedRowVersion, dto);
                Response.Headers["ETag"] = $"\"{updated.ETag}\"";
                return Ok(updated);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating cost {CostId}", costId);
                return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (ConflictException)
            {
                return Conflict(new { message = "Trošak je promenjen od strane drugog korisnika" });
            }
            catch (NotFoundException)
            {
                return NotFound(new { message = "Trošak nije pronađen" });
            }
        }

        [HttpDelete("{costId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCost(int documentId, int costId)
        {
            var deleted = await _costService.DeleteCostAsync(documentId, costId);
            return deleted ? NoContent() : NotFound(new { message = "Trošak nije pronađen" });
        }

        [HttpGet("{costId:int}/items")]
        [ProducesResponseType(typeof(IReadOnlyList<DocumentCostItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<DocumentCostItemDto>>> GetCostItems(int documentId, int costId)
        {
            var items = await _costService.GetCostItemsAsync(documentId, costId);
            return Ok(items);
        }

        [HttpGet("{costId:int}/items/{itemId:int}")]
        [ProducesResponseType(typeof(DocumentCostItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentCostItemDto>> GetCostItem(int documentId, int costId, int itemId)
        {
            var item = await _costService.GetCostItemByIdAsync(documentId, costId, itemId);
            if (item == null)
            {
                return NotFound(new { message = "Stavka troška nije pronađena" });
            }

            Response.Headers["ETag"] = $"\"{item.ETag}\"";
            return Ok(item);
        }

        [HttpPost("{costId:int}/items")]
        [ProducesResponseType(typeof(DocumentCostItemDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentCostItemDto>> CreateCostItem(int documentId, int costId, [FromBody] CreateDocumentCostItemDto dto)
        {
            try
            {
                var created = await _costService.CreateCostItemAsync(documentId, costId, dto);
                Response.Headers["ETag"] = $"\"{created.ETag}\"";
                return CreatedAtAction(nameof(GetCostItem), new { documentId, costId, itemId = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating cost item {CostId}", costId);
                return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (NotFoundException)
            {
                return NotFound(new { message = "Trošak nije pronađen" });
            }
        }

        [HttpPatch("{costId:int}/items/{itemId:int}")]
        [ProducesResponseType(typeof(DocumentCostItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DocumentCostItemDto>> UpdateCostItem(int documentId, int costId, int itemId, [FromBody] PatchDocumentCostItemDto dto)
        {
            try
            {
                var expectedRowVersion = ExtractRowVersionFromIfMatch();
                if (expectedRowVersion == null)
                {
                    return BadRequest(new { message = "Missing or invalid If-Match header" });
                }

                var updated = await _costService.UpdateCostItemAsync(documentId, costId, itemId, expectedRowVersion, dto);
                Response.Headers["ETag"] = $"\"{updated.ETag}\"";
                return Ok(updated);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating cost item {CostItemId}", itemId);
                return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (ConflictException)
            {
                return Conflict(new { message = "Stavka troška je promenjena" });
            }
            catch (NotFoundException)
            {
                return NotFound(new { message = "Stavka troška nije pronađena" });
            }
        }

        [HttpDelete("{costId:int}/items/{itemId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCostItem(int documentId, int costId, int itemId)
        {
            var deleted = await _costService.DeleteCostItemAsync(documentId, costId, itemId);
            return deleted ? NoContent() : NotFound(new { message = "Stavka troška nije pronađena" });
        }

        [HttpPost("{costId:int}/distribute")]
        [ProducesResponseType(typeof(CostDistributionResultDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CostDistributionResultDto>> DistributeCost(int documentId, int costId, [FromBody] CostDistributionRequestDto dto)
        {
            var result = await _costService.DistributeCostAsync(documentId, costId, dto);
            return Ok(result);
        }

        private byte[]? ExtractRowVersionFromIfMatch()
        {
            var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                _logger.LogWarning("Missing If-Match header for cost endpoint");
                return null;
            }

            try
            {
                return Convert.FromBase64String(ifMatch.Trim('"'));
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Invalid ETag format for cost endpoint: {IfMatch}", ifMatch);
                return null;
            }
        }
    }
}
