using System;
using ERPAccounting.Domain.Interfaces;

namespace ERPAccounting.Domain.Entities;

/// <summary>
/// Base entity with audit fields
/// </summary>
public abstract class BaseEntity : IEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}