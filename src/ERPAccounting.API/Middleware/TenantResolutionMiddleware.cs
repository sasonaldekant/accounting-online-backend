namespace ERPAccounting.API.Middleware;

/// <summary>
/// Čita X-Tenant-Id header i prosleđuje ga kroz HttpContext.Items
/// </summary>
public class TenantResolutionMiddleware
{
    private const string HeaderName = "X-Tenant-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var tenantValue) || string.IsNullOrWhiteSpace(tenantValue))
        {
            tenantValue = "default";
            _logger.LogDebug("Tenant header missing. Using default tenant.");
        }

        context.Items[HeaderName] = tenantValue.ToString();
        await _next(context);
    }
}
