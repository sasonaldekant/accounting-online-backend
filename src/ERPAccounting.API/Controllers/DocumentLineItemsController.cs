using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ERPAccounting.API.Controllers
{
    /// <summary>
    /// Document Line Items Controller
    /// KRITIČNO: Implementira ETag konkurentnost mehanizam sa RowVersion
    /// 
    /// WORKFLOW:
    /// 1. GET /api/v1/documents/{id}/items/{itemId}
    ///    Response: ETag header sa Base64(RowVersion)
    /// 
    /// 2. Korisnik deli stavku na nekoliko sekundi
    /// 
    /// 3. PATCH /api/v1/documents/{id}/items/{itemId}
    ///    Header: If-Match: \"{BASE64_ETAG}\"  (OBAVEZNO!)
    ///    Ako RowVersion != If-Match => 409 Conflict
    ///    Ako OK => Novi ETag u response
    /// </summary>
    [ApiController]
    [Route("api/v1/documents/{documentId:int}/items")]
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

        // ══════════════════════════════════════════════════
        // GET OPERACIJE
        
        /// <summary>
        /// GET /api/v1/documents/{documentId}/items
        /// Vraća sve stavke dokumenta sa ETag
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<DocumentLineItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DocumentLineItemDto>>> GetItems(int documentId)
        {
            try
            {
                var items = await _service.GetItemsAsync(documentId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items for document {DocumentId}", documentId);
                return StatusCode(500, new { message = "Greška pri učitavanju stavki" });
            }
        }

        /// <summary>
        /// GET /api/v1/documents/{documentId}/items/{itemId}
        /// Vraća jednu stavku sa ETag u response header-u
        /// Response header: ETag: \"{BASE64_ROWVERSION}\"
        /// </summary>
        [HttpGet("{itemId:int}")]
        [ProducesResponseType(typeof(DocumentLineItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentLineItemDto>> GetItem(int documentId, int itemId)
        {
            try
            {
                var item = await _service.GetAsync(documentId, itemId);

                if (item == null)
                    return NotFound(new { message = "Stavka nije pronađena" });

                Response.Headers["ETag"] = $"\"{item.ETag}\"";
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item {ItemId}", itemId);
                return StatusCode(500, new { message = "Greška pri učitavanju stavke" });
            }
        }

        // ══════════════════════════════════════════════════
        // CREATE OPERACIJA

        /// <summary>
        /// POST /api/v1/documents/{documentId}/items
        /// Kreiraj novu stavku dokumenta
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DocumentLineItemDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentLineItemDto>> CreateItem(int documentId, [FromBody] CreateLineItemDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(documentId, dto);
                Response.Headers["ETag"] = $"\"{created.ETag}\"";
                return CreatedAtAction(nameof(GetItem), new { documentId, itemId = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating item for document {DocumentId}", documentId);
                return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Dokument nije pronađen" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item for document {DocumentId}", documentId);
                return StatusCode(500, new { message = "Greška pri kreiranju stavke" });
            }
        }

        // ══════════════════════════════════════════════════
        // PATCH OPERACIJA - KRITIČNA ZA KONKURENTNOST!

        /// <summary>
        /// PATCH /api/v1/documents/{documentId}/items/{itemId}
        /// Ažurira stavku sa ETag konkurentnosti
        /// 
        /// OBAVEZNO: Header If-Match sa ETag vrednosti
        /// If-Match: \"{BASE64_ROWVERSION}\"
        /// 
        /// Odgovori:
        /// 200 OK - Ažuriranje uspešno, novi ETag u response
        /// 409 Conflict - RowVersion mismatch (stavka promenjena)
        /// </summary>
        [HttpPatch("{itemId:int}")]
        [ProducesResponseType(typeof(DocumentLineItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentLineItemDto>> UpdateItem(
            int documentId,
            int itemId,
            [FromBody] PatchLineItemDto dto)
        {
            try
            {
                var expectedRowVersion = ExtractRowVersionFromIfMatch();
                if (expectedRowVersion == null)
                {
                    return BadRequest(new { message = "Missing or invalid If-Match header" });
                }

                var updated = await _service.UpdateAsync(documentId, itemId, expectedRowVersion, dto);
                Response.Headers["ETag"] = $"\"{updated.ETag}\"";
                return Ok(updated);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating item {ItemId}", itemId);
                return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict for item {ItemId}", itemId);
                return Conflict(new { message = "Stavka je promenjena od drugog korisnika" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Stavka nije pronađena" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId}", itemId);
                return StatusCode(500, new { message = "Greška pri ažuriranju stavke" });
            }
        }

        // ══════════════════════════════════════════════════
        // DELETE OPERACIJA

        /// <summary>
        /// DELETE /api/v1/documents/{documentId}/items/{itemId}
        /// Obriši stavku (soft delete)
        /// </summary>
        [HttpDelete("{itemId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteItem(int documentId, int itemId)
        {
            try
            {
                var deleted = await _service.DeleteAsync(documentId, itemId);
                return deleted ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {ItemId}", itemId);
                return StatusCode(500, new { message = "Greška pri brisanju stavke" });
            }
        }

        private byte[]? ExtractRowVersionFromIfMatch()
        {
            var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                _logger.LogWarning("Missing If-Match header for PATCH");
                return null;
            }

            try
            {
                var etagValue = ifMatch.Trim('"');
                return Convert.FromBase64String(etagValue);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Invalid ETag format: {ETag}", ifMatch);
                return null;
            }
        }
    }
}
