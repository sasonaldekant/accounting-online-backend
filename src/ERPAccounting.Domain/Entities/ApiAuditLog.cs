using System;
using System.Collections.Generic;

namespace ERPAccounting.Domain.Entities
{
    /// <summary>
    /// Entitet za logovanje svih API poziva.
    /// Koristi se za audit trail, debugging i compliance.
    /// </summary>
    public class ApiAuditLog
    {
        public int IDAuditLog { get; set; }

        // Request Info
        public DateTime Timestamp { get; set; }
        public string? HttpMethod { get; set; }
        public string? Endpoint { get; set; }
        public string? RequestPath { get; set; }
        public string? QueryString { get; set; }

        // User Info
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }

        // Request/Response
        public string? RequestBody { get; set; }
        public int ResponseStatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public int? ResponseTimeMs { get; set; }

        // Entity Changes
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? OperationType { get; set; }

        // Error Info
        public bool? IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ExceptionDetails { get; set; }

        // Metadata
        public Guid? CorrelationId { get; set; }
        public string? SessionId { get; set; }

        // Navigation
        public ICollection<ApiAuditLogEntityChange>? EntityChanges { get; set; }
    }
}