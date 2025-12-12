using System.Linq;
using ERPAccounting.API.Helpers;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.DTOs.Documents;
using ERPAccounting.Application.Services;
using ERPAccounting.Common.Exceptions;
using ERPAccounting.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPAccounting.API.Controllers
{
    /// <summary>
    /// Dokument zaglavlja sa ETag/If-Match podrškom
    /// </summary>
    [ApiController]
    [Route("api/v1/documents")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<DocumentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<DocumentDto>>> GetDocuments([FromQuery] DocumentQueryParameters query)
        {
            var result = await _documentService.GetDocumentsAsync(query);
            Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
            return Ok(result);
        }

        /// <summary>
        /// Pretraga dokumenata sa naprednijim filterima
        /// </summary>
        /// <param name="searchDto">Parametri pretrage</param>
        /// <returns>Paginovana lista dokumenata</returns>
        [HttpPost("search")]
        [ProducesResponseType(typeof(PaginatedResult<DocumentDto>), StatusCodes.Status200OK)] // FIXED: Use PaginatedResult<DocumentDto> instead of DocumentSearchResultDto
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaginatedResult<DocumentDto>>> SearchDocuments([FromBody] DocumentSearchDto searchDto) // FIXED: Return PaginatedResult<DocumentDto>
        {
            try
            {
                // Validacija: Bar jedan filter mora biti unet
                if (string.IsNullOrWhiteSpace(searchDto.DocumentNumber) &&
                    !searchDto.PartnerId.HasValue &&
                    !searchDto.DateFrom.HasValue &&
                    !searchDto.DateTo.HasValue &&
                    !searchDto.StatusId.HasValue &&
                    string.IsNullOrWhiteSpace(searchDto.DocumentTypeCode))
                {
                    return BadRequest(new { message = "Unesite bar jedan filter za pretragu" });
                }

                var result = await _documentService.SearchDocumentsAsync(searchDto);
                Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
                Response.Headers["X-Page-Number"] = result.Page.ToString(); // FIXED: Use Page instead of PageNumber
                Response.Headers["X-Page-Size"] = result.PageSize.ToString();
                
                var totalPages = (result.TotalCount + result.PageSize - 1) / result.PageSize;
                Response.Headers["X-Total-Pages"] = totalPages.ToString();
                
                _logger.LogInformation(
                    "Document search: {Filters} returned {Count} results (page {Page}/{TotalPages})",
                    new { searchDto.DocumentNumber, searchDto.PartnerId, searchDto.DateFrom, searchDto.DateTo },
                    result.TotalCount,
                    result.Page,
                    totalPages);

                return Ok(result);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during document search");
                return BadRequest(ProblemDetailsDto.FromException(ex, HttpContext.TraceIdentifier));
            }
        }

        [HttpGet("{documentId:int}")]
        [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentDto>> GetDocument(int documentId)
        {
            var document = await _documentService.GetDocumentByIdAsync(documentId);
            if (document == null)
            {
                return NotFound(new { message = "Dokument nije pronađen" });
            }

            Response.Headers["ETag"] = $"\"{document.ETag}\"";
            return Ok(document);
        }

        [HttpPost]
        [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentDto>> CreateDocument([FromBody] CreateDocumentDto dto)
        {
            try
            {
                var created = await _documentService.CreateDocumentAsync(dto);
                Response.Headers["ETag"] = $"\"{created.ETag}\"";
                return CreatedAtAction(nameof(GetDocument), new { documentId = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating document");
                return BadRequest(ProblemDetailsDto.FromException(ex, HttpContext.TraceIdentifier));
            }
        }

        [HttpPut("{documentId:int}")]
        [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DocumentDto>> UpdateDocument(int documentId, [FromBody] UpdateDocumentDto dto)
        {
            try
            {
                if (!IfMatchHeaderParser.TryExtractRowVersion(
                        HttpContext,
                        _logger,
                        "document update",
                        out var expectedRowVersion,
                        out var problemDetails))
                {
                    return BadRequest(problemDetails);
                }

                var updated = await _documentService.UpdateDocumentAsync(documentId, expectedRowVersion!, dto);
                Response.Headers["ETag"] = $"\"{updated.ETag}\"";
                return Ok(updated);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating document {DocumentId}", documentId);
                return BadRequest(ProblemDetailsDto.FromException(ex, HttpContext.TraceIdentifier));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Dokument je promenjen od strane drugog korisnika" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Dokument nije pronađen" });
            }
        }

        [HttpDelete("{documentId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            var deleted = await _documentService.DeleteDocumentAsync(documentId);
            return deleted ? NoContent() : NotFound(new { message = "Dokument nije pronađen" });
        }

    }
}
