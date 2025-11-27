using ERPAccounting.Domain.Entities;

namespace ERPAccounting.Common.Interfaces
{
    /// <summary>
    /// Servis za logovanje API poziva u bazu podataka.
    /// Koristi se za audit trail, debugging i compliance.
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Kreira novi audit log zapis (pre izvršavanja requesta).
        /// </summary>
        /// <param name="auditLog">Audit log objekat sa request podacima</param>
        Task LogAsync(ApiAuditLog auditLog);

        /// <summary>
        /// Ažurira postojeći audit log sa response podacima (posle izvršavanja requesta).
        /// </summary>
        /// <param name="auditLog">Audit log sa ažuriranim response podacima</param>
        Task UpdateAsync(ApiAuditLog auditLog);

        /// <summary>
        /// Loguje field-level izmene entiteta.
        /// </summary>
        /// <param name="auditLogId">ID parent audit log-a</param>
        /// <param name="entityType">Tip entiteta (npr. "Document", "Partner")</param>
        /// <param name="entityId">ID entiteta koji je promenjen</param>
        /// <param name="operationType">Tip operacije (Create, Update, Delete)</param>
        /// <param name="changes">Dictionary sa izmenjenim property-jima i njihovim vrednostima</param>
        Task LogEntityChangeAsync(
            int auditLogId,
            string entityType,
            string entityId,
            string operationType,
            Dictionary<string, (object OldValue, object NewValue)> changes);
    }
}