using System;

namespace ERPAccounting.Domain.Interfaces;

/// <summary>
/// Represents an entity that tracks audit metadata.
/// Updated to use string for CreatedBy/UpdatedBy to support username tracking.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    string? CreatedBy { get; }
    string? UpdatedBy { get; }
}