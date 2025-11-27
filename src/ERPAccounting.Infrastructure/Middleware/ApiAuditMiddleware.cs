using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ERPAccounting.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware za automatsko logovanje svih API poziva sa JSON snapshot podrškom.
    /// Hvataj request/response i čuva u tblAPIAuditLog tabelu.
    /// 
    /// NOVI PRISTUP:
    /// - Postavlja _currentAuditLogId na AppDbContext
    /// - SaveChangesAsync automatski hvata JSON snapshots iz ChangeTracker-a
    /// - JSON se čuva u tblAPIAuditLogEntityChanges
    /// </summary>
    public class ApiAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiAuditMiddleware> _logger;

        public ApiAuditMiddleware(RequestDelegate next, ILogger<ApiAuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IAuditLogService auditLogService,
            ICurrentUserService currentUserService,
            AppDbContext dbContext)
        {
            var request = context.Request;
            var originalBodyStream = context.Response.Body;

            // 1. Pripremi audit log pre izvršavanja requesta
            var auditLog = new ApiAuditLog
            {
                Timestamp = DateTime.UtcNow,
                HttpMethod = request.Method,
                Endpoint = request.Path.Value ?? string.Empty,
                RequestPath = request.Path.Value ?? string.Empty,
                QueryString = request.QueryString.HasValue ? request.QueryString.Value : null,
                IPAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = request.Headers["User-Agent"].ToString(),
                UserId = currentUserService.UserId,
                Username = currentUserService.Username,
                CorrelationId = Guid.NewGuid()
            };

            // 2. Pročitaj request body (ako je moguće) – bez zatvaranja streama
            if (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH")
            {
                if (request.ContentLength > 0)
                {
                    request.EnableBuffering();

                    if (request.Body.CanSeek)
                    {
                        request.Body.Seek(0, SeekOrigin.Begin);
                    }

                    using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
                    {
                        auditLog.RequestBody = await reader.ReadToEndAsync();
                    }

                    if (request.Body.CanSeek)
                    {
                        request.Body.Seek(0, SeekOrigin.Begin);
                    }
                }
            }

            // 3. Privremeni stream za response
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            var stopwatch = Stopwatch.StartNew();
            int auditLogId = 0;

            try
            {
                // 4. Kreiraj audit log I DOBIJ ID
                await auditLogService.LogAsync(auditLog);
                auditLogId = auditLog.IDAuditLog; // EF postavlja ID nakon insert-a

                // 5. KRITIČNO: Postavi audit log ID na DbContext
                // Ovo omogućava SaveChangesAsync da zna gde da loguje promene
                if (auditLogId > 0)
                {
                    dbContext.SetCurrentAuditLogId(auditLogId);
                }

                // 6. Izvrši request pipeline (ovde se događa SaveChangesAsync sa audit tracking-om)
                await _next(context);

                stopwatch.Stop();

                // 7. Nakon uspešnog izvršavanja
                auditLog.ResponseStatusCode = context.Response.StatusCode;
                auditLog.IsSuccess = context.Response.StatusCode >= 200 && context.Response.StatusCode < 400;
                auditLog.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;

                // 8. Pokušaj da pročitaš response body za error responses
                if (responseBodyStream.CanSeek && responseBodyStream.Length > 0)
                {
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true))
                    {
                        // Loguj samo za error responses
                        if (auditLog.IsSuccess == false)
                        {
                            auditLog.ResponseBody = await reader.ReadToEndAsync();
                        }
                    }
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                }

                // 9. Ažuriraj audit log sa response podacima
                await auditLogService.UpdateAsync(auditLog);

                // 10. Vrati response u originalni stream
                if (responseBodyStream.Length > 0)
                {
                    await responseBodyStream.CopyToAsync(originalBodyStream);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // 11. U slučaju exceptiona, upiši detalje
                auditLog.ResponseStatusCode = context.Response.StatusCode > 0 
                    ? context.Response.StatusCode 
                    : StatusCodes.Status500InternalServerError;
                auditLog.IsSuccess = false;
                auditLog.ErrorMessage = ex.Message;
                auditLog.ExceptionDetails = ex.ToString();
                auditLog.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;

                try
                {
                    await auditLogService.UpdateAsync(auditLog);
                }
                catch (Exception auditEx)
                {
                    // Ne dozvoljavamo da audit failure crashuje aplikaciju
                    _logger.LogError(auditEx, "Failed to update audit entry for failed request");
                }

                // 12. Vrati response body u originalni stream ako ima nečega
                if (responseBodyStream.CanSeek && responseBodyStream.Length > 0)
                {
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    await responseBodyStream.CopyToAsync(originalBodyStream);
                }

                // Re-throw originalni exception
                throw;
            }
            finally
            {
                // 13. Uvek vrati originalni stream i očisti privremeni
                context.Response.Body = originalBodyStream;
                await responseBodyStream.DisposeAsync();
            }
        }
    }
}