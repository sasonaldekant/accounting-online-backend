using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERPAccounting.Application.Common.Interfaces;
using ERPAccounting.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ERPAccounting.Infrastructure.Services
{
    /// <summary>
    /// Implementacija servisa za logovanje API poziva.
    /// Koristi ApplicationDbContext za perzistenciju u bazu.
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            IApplicationDbContext context, 
            ILogger<AuditLogService> logger)
        {
            _context = context;
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
                _context.ApiAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync(default);
                
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
                    
                    _context.ApiAuditLogEntityChanges.Add(entityChange);
                }
                
                await _context.SaveChangesAsync(default);
                
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
        private string SerializeValue(object value)
        {
            if (value == null)
                return null;
            
            // Za jednostavne tipove - direktna konverzija
            if (value is string || value.GetType().IsPrimitive || value is DateTime || value is decimal)
                return value.ToString();
            
            // Za complex tipove - JSON serijalizacija
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(value);
            }
            catch
            {
                return value.ToString();
            }
        }
    }
}

// LOKACIJA: src/ERPAccounting.Infrastructure/Services/AuditLogService.cs
// TIP: NOVI FAJL