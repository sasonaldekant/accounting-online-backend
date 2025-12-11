using System.ComponentModel.DataAnnotations;

namespace ERPAccounting.Application.DTOs.Documents;

/// <summary>
/// DTO za pretragu dokumenata
/// Podržava filtriranje po različitim kriterijumima
/// </summary>
public record DocumentSearchDto
{
    /// <summary>
    /// Broj dokumenta za pretragu (parcijalno poklapanje)
    /// </summary>
    [StringLength(50, ErrorMessage = "Broj dokumenta ne može biti duži od 50 karaktera")]
    public string? DocumentNumber { get; init; }

    /// <summary>
    /// ID partnera za filtriranje
    /// </summary>
    public int? PartnerId { get; init; }

    /// <summary>
    /// Početni datum (od)
    /// </summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>
    /// Krajnji datum (do)
    /// </summary>
    public DateTime? DateTo { get; init; }

    /// <summary>
    /// ID statusa dokumenta
    /// </summary>
    public int? StatusId { get; init; }

    /// <summary>
    /// Tip dokumenta (UR, RO, FO, AR, itd.)
    /// </summary>
    [StringLength(10, ErrorMessage = "Tip dokumenta ne može biti duži od 10 karaktera")]
    public string? DocumentTypeCode { get; init; }

    /// <summary>
    /// Broj stranice (početak od 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Broj stranice mora biti veći od 0")]
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Broj rezultata po stranici (max 100)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Veličina stranice mora biti između 1 i 100")]
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Polje za sortiranje
    /// </summary>
    [StringLength(50, ErrorMessage = "Naziv polja za sortiranje ne može biti duži od 50 karaktera")]
    public string? SortBy { get; init; } = "DocumentDate";

    /// <summary>
    /// Smer sortiranja (asc/desc)
    /// </summary>
    [StringLength(4, ErrorMessage = "Smer sortiranja mora biti 'asc' ili 'desc'")]
    public string? SortDirection { get; init; } = "desc";
}

/// <summary>
/// Response DTO za pretragu dokumenata sa paginacijom
/// </summary>
public record DocumentSearchResultDto
{
    /// <summary>
    /// Lista pronađenih dokumenata
    /// </summary>
    public List<DocumentDto> Documents { get; init; } = new();

    /// <summary>
    /// Ukupan broj rezultata
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Trenutna stranica
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Broj rezultata po stranici
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Ukupan broj stranica
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Da li postoji prethodna stranica
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Da li postoji sledeća stranica
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}