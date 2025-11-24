using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace ERPAccounting.API.Filters;

/// <summary>
/// Action filter koji automatski setuje ETag HTTP header na osnovu DTO property-ja.
/// Eliminise potrebu za ručnim Response.Headers["ETag"] = ... u svakom controller-u.
/// </summary>
public class ETagFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // No-op: ETag se setuje POSLE izvršavanja akcije
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Samo za uspešne response-ove sa rezultatom
        if (context.Result is not ObjectResult objectResult || objectResult.Value == null)
        {
            return;
        }

        // Ne setuj ETag na error response-ove
        var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;
        if (statusCode < 200 || statusCode >= 300)
        {
            return;
        }

        // Pokušaj izvući ETag property iz DTO-a
        var etagValue = ExtractETagFromDto(objectResult.Value);
        if (!string.IsNullOrWhiteSpace(etagValue))
        {
            // RFC 7232: ETag mora biti u navodnicima
            context.HttpContext.Response.Headers["ETag"] = $"\"{etagValue}\"";
        }
    }

    private static string? ExtractETagFromDto(object dto)
    {
        // 1. Proveravaj da li je DTO objekat (ne lista/array)
        var dtoType = dto.GetType();

        // 2. Ako je lista/collection, pokušaj uzeti prvi element
        if (dto is System.Collections.IEnumerable enumerable && dto is not string)
        {
            var enumerator = enumerable.GetEnumerator();
            if (enumerator.MoveNext())
            {
                return ExtractETagFromDto(enumerator.Current!);
            }
            return null; // Prazna kolekcija
        }

        // 3. Traži "ETag" property (case-insensitive)
        var etagProperty = dtoType.GetProperty("ETag", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (etagProperty != null && etagProperty.PropertyType == typeof(string))
        {
            return etagProperty.GetValue(dto) as string;
        }

        // 4. Alternativa: traži "RowVersion" property (ako nije konvertovan u ETag)
        var rowVersionProperty = dtoType.GetProperty("RowVersion", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (rowVersionProperty != null && rowVersionProperty.PropertyType == typeof(byte[]))
        {
            var rowVersion = rowVersionProperty.GetValue(dto) as byte[];
            return rowVersion != null ? Convert.ToBase64String(rowVersion) : null;
        }

        return null;
    }
}
