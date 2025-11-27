using ERPAccounting.Domain.Entities;

namespace ERPAccounting.Common.Interfaces
{
    /// <summary>
    /// Servis za logovanje API poziva u bazu podataka.
    /// Koristi se za audit trail, debugging i compliance.
    /// 
    /// NOVI PRISTUP:
    /// - Podržava JSON snapshot čuvanje celokupnog stanja entiteta
    /// - Automatski detektuje tip operacije iz HTTP metode
    /// - Ne zahteva izmene postojećih entiteta
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Kreira novi audit log zapis (pre izvršavanja requesta).
        /// Automatski detektuje tip operacije iz HTTP metode.
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
        /// <param name="operationType">Tip operacije (Insert, Update, Delete)</param>
        /// <param name="changes">Dictionary sa izmenjenim property-jima i njihovim vrednostima</param>
        Task LogEntityChangeAsync(
            int auditLogId,
            string entityType,
            string entityId,
            string operationType,
            Dictionary<string, (object OldValue, object NewValue)> changes);

        /// <summary>
        /// NOVA METODA: Loguje kompletni JSON snapshot entiteta.
        /// Koristi se za čuvanje celokupnog stanja pre/posle izmene.
        /// JSON se čuva u OldValue/NewValue kolonama sa PropertyName = "__FULL_SNAPSHOT__".
        /// </summary>
        /// <param name="auditLogId">ID parent audit log-a</param>
        /// <param name="entityType">Tip entiteta (npr. "Document", "DocumentLineItem")</param>
        /// <param name="entityId">ID entiteta</param>
        /// <param name="operationType">Tip operacije (Insert, Update, Delete)</param>
        /// <param name="oldState">Staro stanje entiteta (null za Insert)</param>
        /// <param name="newState">Novo stanje entiteta (null za Delete)</param>
        Task LogEntitySnapshotAsync(
            int auditLogId,
            string entityType,
            string entityId,
            string operationType,
            object? oldState,
            object? newState);
    }
}