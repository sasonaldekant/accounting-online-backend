using System;
using ERPAccounting.Application.DTOs.Documents;
using ERPAccounting.Application.DTOs.LineItems;
using ERPAccounting.Domain.Entities;

namespace ERPAccounting.Application.Mapping;

/// <summary>
/// Extension metode za mapiranje Entity <-> DTO
/// </summary>
public static class MappingExtensions
{
    // ============================================================
    // DOCUMENT MAPPING
    // ============================================================
    
    public static DocumentDto ToDto(this Document document)
    {
        return new DocumentDto(
            document.Id,
            document.BrojDokumenta,
            document.Datum,
            document.PartnerId,
            document.OrganizacionaJedinicaId,
            document.RadnikId,
            document.ValutaId,
            document.KursValute,
            document.NacinOporezivanjaId,
            document.ReferentniDokumentId,
            document.Napomena,
            document.ObracunAkciza,
            document.ObracunPorez,
            document.Procesiran,
            document.CreatedAt,
            document.UpdatedAt,
            document.CreatedBy,
            document.UpdatedBy
        );
    }
    
    public static Document ToEntity(this CreateDocumentDto dto, int userId)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            BrojDokumenta = dto.BrojDokumenta,
            Datum = dto.Datum,
            PartnerId = dto.PartnerId,
            OrganizacionaJedinicaId = dto.OrganizacionaJedinicaId,
            RadnikId = dto.RadnikId,
            ValutaId = dto.ValutaId,
            KursValute = dto.KursValute,
            NacinOporezivanjaId = dto.NacinOporezivanjaId,
            ReferentniDokumentId = dto.ReferentniDokumentId,
            Napomena = dto.Napomena,
            ObracunAkciza = dto.ObracunAkciza,
            ObracunPorez = dto.ObracunPorez,
            Procesiran = false,
            CreatedBy = userId,
            UpdatedBy = userId
        };
    }
    
    // ============================================================
    // LINE ITEM MAPPING
    // ============================================================
    
    /// <summary>
    /// KRITIČNO: ToDto sa ETag (Base64 encoded RowVersion)
    /// </summary>
    public static DocumentLineItemDto ToDto(this DocumentLineItem item)
    {
        return new DocumentLineItemDto(
            item.Id,
            item.DocumentId,
            item.ArticleId,
            item.Quantity,
            item.Price,
            item.Discount,
            item.Margin,
            item.VatRate,
            item.CalculateExcise,
            item.CalculateTax,
            item.CreatedAt,
            item.UpdatedAt,
            item.CreatedBy,
            item.UpdatedBy,
            Convert.ToBase64String(item.RowVersion)  // KRITIČNO: ETag
        );
    }
    
    public static DocumentLineItem ToEntity(this CreateLineItemDto dto, Guid documentId, int userId)
    {
        return new DocumentLineItem
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ArticleId = dto.ArticleId,
            Quantity = dto.Quantity,
            Price = dto.Price,
            Discount = dto.Discount,
            Margin = dto.Margin,
            VatRate = dto.VatRate,
            CalculateExcise = dto.CalculateExcise,
            CalculateTax = dto.CalculateTax,
            CreatedBy = userId,
            UpdatedBy = userId
        };
    }
    
    /// <summary>
    /// KRITIČNO: Primeni PATCH izmene na postojeći entitet
    /// </summary>
    public static void ApplyPatch(this DocumentLineItem item, PatchLineItemDto patch, int userId)
    {
        item.Quantity = patch.Quantity;
        item.Price = patch.Price;
        item.Discount = patch.Discount;
        item.Margin = patch.Margin;
        item.VatRate = patch.VatRate;
        item.CalculateExcise = patch.CalculateExcise;
        item.CalculateTax = patch.CalculateTax;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = userId;
    }
    
    // ============================================================
    // ETAG HELPERS
    // ============================================================
    
    /// <summary>
    /// Konvertuj ETag string u byte array
    /// </summary>
    public static byte[] FromETag(string eTag)
    {
        return Convert.FromBase64String(eTag.Trim());
    }
    
    /// <summary>
    /// Konvertuj byte array u ETag string
    /// </summary>
    public static string ToETag(byte[] rowVersion)
    {
        return Convert.ToBase64String(rowVersion);
    }
}
