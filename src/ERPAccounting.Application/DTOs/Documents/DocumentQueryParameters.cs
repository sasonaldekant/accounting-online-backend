namespace ERPAccounting.Application.DTOs.Documents;

/// <summary>
/// Query parametri za listu dokumenata sa paginacijom
/// </summary>
public record DocumentQueryParameters(int Page = 1, int PageSize = 20, string? Search = null);
