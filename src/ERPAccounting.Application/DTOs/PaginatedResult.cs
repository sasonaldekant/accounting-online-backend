using System.Collections.Generic;

namespace ERPAccounting.Application.DTOs;

/// <summary>
/// DTO za povratak paginiranih rezultata
/// </summary>
public record PaginatedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
