using ERPAccounting.API.Helpers;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.Services;
using ERPAccounting.Common.Constants;
using ERPAccounting.Common.Exceptions;
using ERPAccounting.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ERPAccounting.API.Controllers
{
    /// <summary>
    /// Document Line Items Controller
    /// KRITIČNO: Implementira ETag konkurentnost mehanizam sa RowVersion
    /// </summary>
    [ApiController]
    [Route(ApiRoutes.DocumentLineItems.Base)]
    [Authorize]
    public class DocumentLineItemsController : ControllerBase
    {
        private readonly IDocumentLineItemService _service;
        private readonly ILogger<DocumentLineItemsController> _logger;

        public DocumentLineItemsController(
            IDocumentLineItemService service,
            ILogger<DocumentLineItemsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DocumentLineItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DocumentLineItemDto>>> GetItems(int documentId)
        {
            var items = await _service.GetItemsAsync(documentId);
            return Ok(items);
        }

        [HttpGet(ApiRoutes.DocumentLineItems.ItemById)]
        [ProducesResponseType(typeof(DocumentLineItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentLineItemDto>> GetItem(int documentId, int itemId)
        {
            var item = await _service.GetAsync(documentId, itemId);
            if (item is null)
            {
                throw new NotFoundException(ErrorMessages.DocumentLineItemNotFound, itemId.ToString(), nameof(DocumentLineItemDto));
            }

            WriteEtag(item.ETag);
            return Ok(item);
        }

        [HttpPost]
        [ProducesResponseType(typeof(DocumentLineItemDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentLineItemDto>> CreateItem(int documentId, [FromBody] CreateLineItemDto dto)
        {
            var created = await _service.CreateAsync(documentId, dto);
            WriteEtag(created.ETag);
            return CreatedAtAction(nameof(GetItem), new { documentId, itemId = created.Id }, created);
        }

        [HttpPatch(ApiRoutes.DocumentLineItems.ItemById)]
        [ProducesResponseType(typeof(DocumentLineItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentLineItemDto>> UpdateItem(
            int documentId,
            int itemId,
            [FromBody] PatchLineItemDto dto)
        {
            if (!IfMatchHeaderParser.TryExtractRowVersion(
                    HttpContext,
                    _logger,
                    "document line item PATCH",
                    out var expectedRowVersion,
                    out var problemDetails))
            {
                return BadRequest(problemDetails);
            }

            var updated = await _service.UpdateAsync(documentId, itemId, expectedRowVersion!, dto);
            WriteEtag(updated.ETag);
            return Ok(updated);
        }

        [HttpDelete(ApiRoutes.DocumentLineItems.ItemById)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetailsDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteItem(int documentId, int itemId)
        {
            await _service.DeleteAsync(documentId, itemId);
            return NoContent();
        }

        private void WriteEtag(string? etag)
        {
            if (!string.IsNullOrWhiteSpace(etag))
            {
                Response.Headers["ETag"] = $"\"{etag}\"";
            }
        }

        private bool TryExtractRowVersion(out byte[]? rowVersion, out ProblemDetailsDto? problem)
        {
            return IfMatchHeaderParser.TryExtractRowVersion(
                HttpContext,
                _logger,
                "document line item PATCH",
                out rowVersion,
                out problem);
        }
    }
}
