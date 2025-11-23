using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERPAccounting.Infrastructure.Services
{
    /// <summary>
    /// Implementacija servisa za logovanje API poziva.
    /// Koristi AppDbContext za perzistenciju u bazu.
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            IDbContextFactory<AppDbContext> contextFactory,
            ILogger<AuditLogService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Asinkrono loguje API poziv u bazu.
        /// Ne baca exception ako logovanje faila - samo loguje error.
        /// </summary>
        public async Task LogAsync(ApiAuditLog auditLog)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                context.ApiAuditLogs.Add(auditLog);
                await context.SaveChangesAsync(default);

                _logger.LogDebug(
                    "API call logged: {Method} {Endpoint} - {StatusCode} ({ResponseTime}ms)",
                    auditLog.HttpMethod,
                    auditLog.Endpoint,
                    auditLog.ResponseStatusCode,
                    auditLog.ResponseTimeMs);
            }
            catch (Exception ex)
            {
                // KRITIČNO: Ne bacaj exception - audit failure ne sme da prekine request
                _logger.LogError(ex,
                    "Failed to log API audit entry for {Method} {Endpoint}",
                    auditLog.HttpMethod,
                    auditLog.Endpoint);
                // Opciono: Fallback na file logging ili alternative storage
                // LogToFile(auditLog);
            }
        }

        /// <summary>
        /// Loguje field-level izmene entiteta.
        /// </summary>
        public async Task LogEntityChangeAsync(
            int auditLogId,
            string entityType,
            string entityId,
            string operationType,
            Dictionary<string, (object OldValue, object NewValue)> changes)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                if (changes == null || changes.Count == 0)
                {
                    _logger.LogWarning(
                        "No changes to log for {EntityType} {EntityId}",
                        entityType,
                        entityId);
                    return;
                }

                foreach (var change in changes)
                {
                    var entityChange = new ApiAuditLogEntityChange
                    {
                        IDAuditLog = auditLogId,
                        PropertyName = change.Key,
                        OldValue = SerializeValue(change.Value.OldValue),
                        NewValue = SerializeValue(change.Value.NewValue),
                        DataType = change.Value.NewValue?.GetType().Name ?? "null"
                    };

                    context.ApiAuditLogEntityChanges.Add(entityChange);
                }

                await context.SaveChangesAsync(default);

                _logger.LogDebug(
                    "Logged {Count} field changes for {EntityType} {EntityId}",
                    changes.Count,
                    entityType,
                    entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to log entity changes for {EntityType} {EntityId}",
                    entityType,
                    entityId);
            }
        }

        /// <summary>
        /// Helper metoda za serijalizaciju vrednosti u string format.
        /// Rukuje null, complex types, i edge cases.
        /// </summary>
        private static string? SerializeValue(object value)
        {
            if (value == null)
                return null;

            // Za jednostavne tipove - direktna konverzija
            if (value is string || value.GetType().IsPrimitive || value is DateTime || value is decimal)
                return value.ToString() ?? string.Empty;

            // Za complex tipove - JSON serijalizacija
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(value);
            }
            catch
            {
                return value.ToString() ?? string.Empty;
            }
        }
    }
}