using System;

namespace ERPAccounting.Domain.ValueObjects;

/// <summary>
/// Represents a status coming from the legacy ERP tables.
/// Provides common, descriptive statuses plus a factory for custom combinations.
/// </summary>
public sealed class DocumentStatus : IEquatable<DocumentStatus>
{
    public int Id { get; }
    public string Name { get; }
    public bool IsFinal { get; }

    private DocumentStatus(int id, string name, bool isFinal)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Status name must be provided.", nameof(name));
        }

        Id = id;
        Name = name;
        IsFinal = isFinal;
    }

    public static DocumentStatus Draft { get; } = new(0, "Draft", false);
    public static DocumentStatus Processed { get; } = new(1, "Processed", false);
    public static DocumentStatus Posted { get; } = new(2, "Posted", true);
    public static DocumentStatus Cancelled { get; } = new(3, "Cancelled", true);

    public static DocumentStatus FromDatabase(int id, string name, bool isFinal)
        => new(id, name, isFinal);

    public bool Equals(DocumentStatus? other) => other is not null && Id == other.Id;

    public override bool Equals(object? obj) => obj is DocumentStatus status && Equals(status);

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString() => $"{Id} - {Name}";
}
