using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace ERPAccounting.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware za automatsko logovanje svih API poziva.
    /// Hvataj request/response i čuva u tblAPIAuditLog tabelu.
    /// </summary>
    public class ApiAuditMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiAuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            IAuditLogService auditLogService,
            ICurrentUserService currentUserService)
        {
            // Kreiraj audit log objekat
            var auditLog = new ApiAuditLog
            {
                Timestamp = DateTime.UtcNow,
                HttpMethod = context.Request.Method,
                Endpoint = context.Request.Path,
                RequestPath = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                IPAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                Username = currentUserService.Username,
                UserId = currentUserService.UserId,
                CorrelationId = Guid.NewGuid()
            };

            // Capture request body za POST/PUT/PATCH operacije
            if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH")
            {
                context.Request.EnableBuffering();
                
                using (var reader = new StreamReader(
                    context.Request.Body, 
                    Encoding.UTF8, 
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 4096,
                    leaveOpen: true))
                {
                    auditLog.RequestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0; // Reset stream za dalje procesiranje
                }
            }

            // Measure response time
            var stopwatch = Stopwatch.StartNew();
            
            // Capture response body
            var originalBodyStream = context.Response.Body;
            
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                try
                {
                    // Pozovi sledeći middleware u pipeline
                    await _next(context);
                    
                    stopwatch.Stop();
                    
                    // Populate response info
                    auditLog.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    auditLog.ResponseStatusCode = context.Response.StatusCode;
                    auditLog.IsSuccess = context.Response.StatusCode < 400;

                    // Capture response body (opciono - samo za error responses)
                    responseBody.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(responseBody))
                    {
                        // Loguj samo za error responses
                        if (auditLog.IsSuccess == false)
                        {
                            auditLog.ResponseBody = await reader.ReadToEndAsync();
                        }
                    }
                    
                    // Copy response back to original stream
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    
                    // Log exception info
                    auditLog.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    auditLog.IsSuccess = false;
                    auditLog.ErrorMessage = ex.Message;
                    auditLog.ExceptionDetails = ex.ToString();
                    auditLog.ResponseStatusCode = 500;
                    
                    // Re-throw exception - audit ne sme da proguta greške
                    throw;
                }
                finally
                {
                    // VAŽNO: Loguj asinkrono da ne blokiraš request
                    // Fire and forget pattern - ne čekamo da se logovanje završi
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await auditLogService.LogAsync(auditLog);
                        }
                        catch
                        {
                            // Ignore errors - audit failure ne sme da crash-uje aplikaciju
                            // AuditLogService već loguje greške u svoj logger
                        }
                    });
                }
            }
        }
    }
}
