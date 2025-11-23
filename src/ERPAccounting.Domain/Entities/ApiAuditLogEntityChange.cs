namespace ERPAccounting.Domain.Entities
{
    /// <summary>
    /// Entitet za praćenje izmena na field-level (kolona po kolona).
    /// Povezan sa ApiAuditLog kao child entitet.
    /// </summary>
    public class ApiAuditLogEntityChange
    {
        public int IDEntityChange { get; set; }
        public int IDAuditLog { get; set; }

        // Change Details
        public string? PropertyName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? DataType { get; set; }

        // Navigation
        public ApiAuditLog? AuditLog { get; set; }
    }
}