using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ERPAccounting.Infrastructure.Services
{
    /// <summary>
    /// Implementacija servisa za logovanje API poziva sa JSON snapshot podrškom.
    /// Koristi AppDbContext za perzistenciju u bazu.
    /// 
    /// NOVI PRISTUP:
    /// - NE menjamo postojeće tabele i entitete
    /// - Koristimo EF ChangeTracker za izvlačenje stanja
    /// - JSON snapshot se čuva u OldValue/NewValue kolonama
    /// - Akcija se zaključuje iz HTTP metode i rute
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly ILogger<AuditLogService> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public AuditLogService(
            IDbContextFactory<AppDbContext> contextFactory,
            ILogger<AuditLogService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Asinkrono loguje API poziv u bazu sa automatskom detekcijom akcije.
        /// Ne baca exception ako logovanje faila - samo loguje error.
        /// </summary>
        public async Task LogAsync(ApiAuditLog auditLog)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Automatski detektuj tip operacije iz HTTP metode i endpoint-a
                auditLog.OperationType = DetermineOperationType(auditLog.HttpMethod, auditLog.Endpoint);

                context.ApiAuditLogs.Add(auditLog);
                await context.SaveChangesAsync(default);

                _logger.LogDebug(
                    "API call logged: {Method} {Endpoint} - Operation: {Operation}",
                    auditLog.HttpMethod,
                    auditLog.Endpoint,
                    auditLog.OperationType);
            }
            catch (Exception ex)
            {
                // KRITIČNO: Ne bacaj exception - audit failure ne sme da prekine request
                _logger.LogError(ex,
                    "Failed to log API audit entry for {Method} {Endpoint}",
                    auditLog.HttpMethod,
                    auditLog.Endpoint);
            }
        }

        /// <summary>
        /// Ažurira postojeći audit log sa response podacima.
        /// ISPRAVKA: Eksplicitno označi ResponseBody kao Modified.
        /// </summary>
        public async Task UpdateAsync(ApiAuditLog auditLog)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var existing = await context.ApiAuditLogs
                    .FirstOrDefaultAsync(a => a.IDAuditLog == auditLog.IDAuditLog);

                if (existing == null)
                {
                    _logger.LogWarning(
                        "Audit log {AuditLogId} not found for update",
                        auditLog.IDAuditLog);
                    return;
                }

                // Ažuriraj response podatke
                existing.ResponseStatusCode = auditLog.ResponseStatusCode;
                existing.ResponseBody = auditLog.ResponseBody;
                existing.ResponseTimeMs = auditLog.ResponseTimeMs;
                existing.IsSuccess = auditLog.IsSuccess;
                existing.ErrorMessage = auditLog.ErrorMessage;
                existing.ExceptionDetails = auditLog.ExceptionDetails;

                // KRITIČNA ISPRAVKA: Eksplicitno markiraj ResponseBody kao Modified
                // EF Change Tracker ne detektuje NULL -> STRING promenu uvek korektno
                context.Entry(existing).Property(e => e.ResponseBody).IsModified = true;
                context.Entry(existing).Property(e => e.RequestBody).IsModified = true;

                await context.SaveChangesAsync(default);

                _logger.LogDebug(
                    "API call updated: {Method} {Endpoint} - {StatusCode} ({ResponseTime}ms) - ResponseBody: {HasBody}",
                    auditLog.HttpMethod,
                    auditLog.Endpoint,
                    auditLog.ResponseStatusCode,
                    auditLog.ResponseTimeMs,
                    string.IsNullOrEmpty(auditLog.ResponseBody) ? "NULL" : "POPULATED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to update API audit entry {AuditLogId}",
                    auditLog.IDAuditLog);
            }
        }

        /// <summary>
        /// Loguje entity changes sa JSON snapshot podrškom.
        /// OldValue/NewValue kolone se koriste za čuvanje kompletnog JSON stanja.
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
                    _logger.LogDebug(
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
                        DataType = change.Value.NewValue?.GetType().Name ?? change.Value.OldValue?.GetType().Name ?? "null"
                    };

                    context.ApiAuditLogEntityChanges.Add(entityChange);
                }

                await context.SaveChangesAsync(default);

                _logger.LogDebug(
                    "Logged {Count} field changes for {EntityType} {EntityId} (Operation: {Operation})",
                    changes.Count,
                    entityType,
                    entityId,
                    operationType);
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
        /// NOVA METODA: Loguje kompletni JSON snapshot entiteta.
        /// Koristi se za čuvanje celokupnog stanja pre/posle izmene.
        /// </summary>
        public async Task LogEntitySnapshotAsync(
            int auditLogId,
            string entityType,
            string entityId,
            string operationType,
            object? oldState,
            object? newState)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var entityChange = new ApiAuditLogEntityChange
                {
                    IDAuditLog = auditLogId,
                    PropertyName = "__FULL_SNAPSHOT__", // Specijalni marker za kompletni snapshot
                    OldValue = oldState != null ? JsonSerializer.Serialize(oldState, _jsonOptions) : null,
                    NewValue = newState != null ? JsonSerializer.Serialize(newState, _jsonOptions) : null,
                    DataType = "JSON"
                };

                context.ApiAuditLogEntityChanges.Add(entityChange);
                await context.SaveChangesAsync(default);

                _logger.LogInformation(
                    "Logged JSON snapshot for {EntityType} {EntityId} (Operation: {Operation})",
                    entityType,
                    entityId,
                    operationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to log entity snapshot for {EntityType} {EntityId}",
                    entityType,
                    entityId);
            }
        }

        /// <summary>
        /// Determiniše tip operacije na osnovu HTTP metode i endpoint-a.
        /// </summary>
        private static string DetermineOperationType(string? httpMethod, string? endpoint)
        {
            if (string.IsNullOrEmpty(httpMethod))
                return "Unknown";

            // Analiza HTTP metode
            return httpMethod.ToUpperInvariant() switch
            {
                "POST" => "Insert",
                "PUT" => "Update",
                "PATCH" => "Update",
                "DELETE" => "Delete",
                "GET" => "Read",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Helper metoda za serijalizaciju vrednosti u string format.
        /// Rukuje null, complex types, i edge cases.
        /// </summary>
        public static string? SerializeValue(object? value)
        {
            if (value is null)
                return null;

            // Za jednostavne tipove - direktna konverzija
            if (value is string str)
                return str;

            if (value.GetType().IsPrimitive || value is DateTime || value is decimal || value is Guid)
                return value.ToString();

            // Za complex tipove - JSON serijalizacija
            try
            {
                return JsonSerializer.Serialize(value, _jsonOptions);
            }
            catch
            {
                return value.ToString();
            }
        }
    }
}