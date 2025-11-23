using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
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
    public class ApiAuditMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

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
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                Username = currentUserService.Username,
                UserId = currentUserService.UserId,
                CorrelationId = Guid.NewGuid()
            };

            // Capture request body za POST/PUT operacije
            if (context.Request.Method == "POST" || context.Request.Method == "PUT")
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(
                    context.Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 4096,
                    leaveOpen: true);
                auditLog.RequestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0; // Reset stream za dalje procesiranje
            }

            // Measure response time
            var stopwatch = Stopwatch.StartNew();

            // Capture response body using a non-disposable wrapper so downstream code cannot close it
            var originalBodyStream = context.Response.Body;
            await using var responseBody = new MemoryStream();
            await using var safeResponseBody = new NonDisposableStream(responseBody);
            context.Response.Body = safeResponseBody;

            try
            {
                // Pozovi sledeći middleware u pipeline
                await _next(context);
                stopwatch.Stop();

                // Populate response info
                auditLog.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                auditLog.ResponseStatusCode = context.Response.StatusCode;
                auditLog.IsSuccess = context.Response.StatusCode < 400;

                // Capture response body (opciono - može biti veliko)
                // Za production, možda ne želiš da loguješ response body
                if (TrySeekToBeginning(responseBody) && auditLog.IsSuccess == false)
                {
                    auditLog.ResponseBody = await ReadResponseBodyAsync(responseBody);
                }

                // Copy response back to original stream
                if (TrySeekToBeginning(responseBody))
                {
                    await CopyResponseAsync(responseBody, originalBodyStream);
                }
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
                // Vrati originalni stream kako bi naredni middleware mogao da piše u response
                context.Response.Body = originalBodyStream;

                try
                {
                    await auditLogService.LogAsync(auditLog);
                }
                catch
                {
                    // Ignore errors - audit failure ne sme da crash-uje aplikaciju
                    // AuditLogService već loguje greške u svoj logger
                }
            }
        }

        private static bool TrySeekToBeginning(Stream stream)
        {
            try
            {
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    return true;
                }
            }
            catch (ObjectDisposedException)
            {
                // Stream je zatvoren od strane downstream middleware-a
            }

            return false;
        }

        private static async Task<string?> ReadResponseBodyAsync(Stream stream)
        {
            try
            {
                if (!stream.CanRead)
                {
                    return null;
                }

                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                return await reader.ReadToEndAsync();
            }
            catch (ObjectDisposedException)
            {
                // Stream je zatvoren, preskoči čitanje
                return null;
            }
        }

        private static async Task CopyResponseAsync(Stream source, Stream destination)
        {
            try
            {
                if (source.CanRead)
                {
                    await source.CopyToAsync(destination);
                }
            }
            catch (ObjectDisposedException)
            {
                // Stream je zatvoren, preskoči kopiranje
            }
        }

        private sealed class NonDisposableStream(Stream inner) : Stream
        {
            public override bool CanRead => inner.CanRead;
            public override bool CanSeek => inner.CanSeek;
            public override bool CanWrite => inner.CanWrite;
            public override long Length => inner.Length;
            public override long Position { get => inner.Position; set => inner.Position = value; }

            public override void Flush() => inner.Flush();
            public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
            public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.ReadAsync(buffer, offset, count, cancellationToken);
            public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
            public override void SetLength(long value) => inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => inner.WriteAsync(buffer, cancellationToken);
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.WriteAsync(buffer, offset, count, cancellationToken);
            public override void WriteByte(byte value) => inner.WriteByte(value);

            protected override void Dispose(bool disposing)
            {
                // Do not dispose the inner stream to avoid breaking middleware that expects it to stay open
            }

            public override async ValueTask DisposeAsync()
            {
                // Mirror Dispose behavior but keep inner stream open
                await Task.CompletedTask;
            }
        }
    }
}