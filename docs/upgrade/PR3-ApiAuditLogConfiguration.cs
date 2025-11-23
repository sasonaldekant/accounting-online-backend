using ERPAccounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPAccounting.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core konfiguracija za ApiAuditLog entitet.
    /// Mapira na tblAPIAuditLog tabelu u bazi.
    /// </summary>
    public class ApiAuditLogConfiguration : IEntityTypeConfiguration<ApiAuditLog>
    {
        public void Configure(EntityTypeBuilder<ApiAuditLog> builder)
        {
            builder.ToTable("tblAPIAuditLog");
            
            // Primary Key
            builder.HasKey(e => e.IDAuditLog);
            
            // Required fields
            builder.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            builder.Property(e => e.HttpMethod)
                .HasMaxLength(10)
                .IsRequired();
            
            builder.Property(e => e.Endpoint)
                .HasMaxLength(500)
                .IsRequired();
            
            builder.Property(e => e.RequestPath)
                .HasMaxLength(500);
            
            builder.Property(e => e.QueryString)
                .HasMaxLength(2000);
            
            // User Info
            builder.Property(e => e.Username)
                .HasMaxLength(100)
                .IsRequired();
            
            builder.Property(e => e.IPAddress)
                .HasMaxLength(50);
            
            builder.Property(e => e.UserAgent)
                .HasMaxLength(500);
            
            // Request/Response bodies - NVARCHAR(MAX)
            builder.Property(e => e.RequestBody)
                .HasColumnType("NVARCHAR(MAX)");
            
            builder.Property(e => e.ResponseBody)
                .HasColumnType("NVARCHAR(MAX)");
            
            builder.Property(e => e.ResponseStatusCode)
                .IsRequired();
            
            // Entity Info
            builder.Property(e => e.EntityType)
                .HasMaxLength(100);
            
            builder.Property(e => e.EntityId)
                .HasMaxLength(50);
            
            builder.Property(e => e.OperationType)
                .HasMaxLength(20)
                .IsRequired();
            
            // Error Info
            builder.Property(e => e.IsSuccess)
                .IsRequired()
                .HasDefaultValue(true);
            
            builder.Property(e => e.ErrorMessage)
                .HasColumnType("NVARCHAR(MAX)");
            
            builder.Property(e => e.ExceptionDetails)
                .HasColumnType("NVARCHAR(MAX)");
            
            // Metadata
            builder.Property(e => e.SessionId)
                .HasMaxLength(100);
            
            // Relationships
            builder.HasMany(e => e.EntityChanges)
                .WithOne(e => e.AuditLog)
                .HasForeignKey(e => e.IDAuditLog)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes su kreirani u SQL migraciji, ali možemo ih definisati i ovde
            builder.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_APIAuditLog_Timestamp");
            
            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_APIAuditLog_UserId");
            
            builder.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasDatabaseName("IX_APIAuditLog_EntityType_EntityId");
            
            builder.HasIndex(e => e.Endpoint)
                .HasDatabaseName("IX_APIAuditLog_Endpoint");
            
            builder.HasIndex(e => e.OperationType)
                .HasDatabaseName("IX_APIAuditLog_OperationType");
        }
    }
}

// LOKACIJA: src/ERPAccounting.Infrastructure/Persistence/Configurations/ApiAuditLogConfiguration.cs
// TIP: NOVI FAJL