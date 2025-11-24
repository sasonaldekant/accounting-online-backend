using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Common.Interfaces;

namespace ERPAccounting.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core DbContext for ERP Accounting system.
    /// Maps all tables and configures relationships.
    /// Database-First approach - entities map to existing tables.
    /// 
    /// AUDIT SYSTEM:
    /// - Entity-level audit (CreatedAt, UpdatedAt, IsDeleted) is NOT used
    /// - All auditing is done via ApiAuditLog + ApiAuditLogEntityChanges tables
    /// - See ApiAuditMiddleware for automatic API request/response logging
    /// </summary>
    public class AppDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ICurrentUserService currentUserService) : base(options)
        {
            _currentUserService = currentUserService;
        }

        // NOTE: Legacy AuditInterceptor has been removed
        // Audit is now handled by:
        // 1. ApiAuditMiddleware - logs all API requests/responses to tblAPIAuditLog
        // 2. AuditLogService - tracks entity changes to tblAPIAuditLogEntityChanges
        // This provides complete audit trail without modifying entity schemas

        // ═══════════════════════════════════════════════════════════════
        // MAIN TABLES
        // ═══════════════════════════════════════════════════════════════
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<DocumentLineItem> DocumentLineItems { get; set; } = null!;
        public DbSet<DocumentCost> DocumentCosts { get; set; } = null!;
        public DbSet<DocumentCostLineItem> DocumentCostLineItems { get; set; } = null!;
        public DbSet<DocumentAdvanceVAT> DocumentAdvanceVATs { get; set; } = null!;
        public DbSet<DependentCostLineItem> DependentCostLineItems { get; set; } = null!;
        public DbSet<DocumentCostVAT> DocumentCostVATs { get; set; } = null!;

        // ═══════════════════════════════════════════════════════════════
        // AUDIT LOG TABLES (new tables for tracking changes)
        // ═══════════════════════════════════════════════════════════════
        public DbSet<ApiAuditLog> ApiAuditLogs { get; set; } = null!;
        public DbSet<ApiAuditLogEntityChange> ApiAuditLogEntityChanges { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // NOTE: Global query filter for ISoftDeletable has been REMOVED.
            // Soft delete is now tracked via ApiAuditLog tables, not entity properties.
            // This prevents "Invalid column name 'IsDeleted'" SQL exceptions.

            // ═══════════════════════════════════════════════════════════════
            // DOCUMENT CONFIGURATION
            // ═══════════════════════════════════════════════════════════════
            var documentEntity = modelBuilder.Entity<Document>();
            documentEntity.HasKey(e => e.IDDokument);
            documentEntity.ToTable("tblDokument");

            // RowVersion for concurrency - MANDATORY!
            documentEntity.Property(e => e.DokumentTimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Foreign keys
            documentEntity.HasMany(e => e.LineItems)
                .WithOne(e => e.Document)
                .HasForeignKey(e => e.IDDokument)
                .OnDelete(DeleteBehavior.Cascade);

            documentEntity.HasMany(e => e.DependentCosts)
                .WithOne(e => e.Document)
                .HasForeignKey(e => e.IDDokument)
                .OnDelete(DeleteBehavior.Cascade);

            documentEntity.HasMany(e => e.AdvanceVATs)
                .WithOne(e => e.Document)
                .HasForeignKey(e => e.IDDokument)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // ═══════════════════════════════════════════════════════════════
            // DOCUMENT LINE ITEM CONFIGURATION - CRITICAL FOR CONCURRENCY
            // ═══════════════════════════════════════════════════════════════
            var lineItemEntity = modelBuilder.Entity<DocumentLineItem>();
            lineItemEntity.HasKey(e => e.IDStavkaDokumenta);
            lineItemEntity.ToTable("tblStavkaDokumenta");

            // RowVersion for concurrency - MANDATORY!
            lineItemEntity.Property(e => e.StavkaDokumentaTimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Money types with precise scale
            lineItemEntity.Property(e => e.Kolicina)
                .HasColumnType("money")
                .HasPrecision(19, 4);

            lineItemEntity.Property(e => e.FakturnaCena)
                .HasColumnType("money")
                .HasPrecision(19, 4);

            lineItemEntity.Property(e => e.NabavnaCena)
                .HasColumnType("money")
                .HasPrecision(19, 4);

            lineItemEntity.Property(e => e.MagacinskaCena)
                .HasColumnType("money")
                .HasPrecision(19, 4);

            lineItemEntity.Property(e => e.RabatDokument)
                .HasColumnType("money")
                .HasPrecision(19, 4);

            lineItemEntity.Property(e => e.Rabat)
                .HasColumnType("money")
                .HasPrecision(19, 4);

            lineItemEntity.Property(e => e.VrednostObracunPDV)
                .HasColumnType("decimal(19, 4)")
                .HasPrecision(19, 4);

            lineItemEntity.Property(e => e.VrednostObracunAkciza)
                .HasColumnType("decimal(19, 4)")
                .HasPrecision(19, 4);

            lineItemEntity.Property(e => e.ProizvodnjaKolicina)
                .HasColumnType("decimal(19, 4)")
                .HasPrecision(19, 4);

            lineItemEntity.Property(e => e.ProizvodnjaKoeficijentKolicine)
                .HasColumnType("decimal(19, 4)")
                .HasPrecision(19, 4);

            // Foreign keys
            lineItemEntity.HasOne(e => e.Document)
                .WithMany(e => e.LineItems)
                .HasForeignKey(e => e.IDDokument)
                .OnDelete(DeleteBehavior.Cascade);

            // ═══════════════════════════════════════════════════════════════
            // DOCUMENT COST CONFIGURATION
            // ═══════════════════════════════════════════════════════════════
            var costEntity = modelBuilder.Entity<DocumentCost>();
            costEntity.HasKey(e => e.IDDokumentTroskovi);
            costEntity.ToTable("tblDokumentTroskovi");

            // RowVersion for concurrency
            costEntity.Property(e => e.DokumentTroskoviTimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Kurs column configuration
            costEntity.Property(e => e.Kurs)
                .HasColumnType("money")
                .HasPrecision(19, 4);

            // NOTE: IznosBezPDV and IznosPDV are [NotMapped] computed properties
            // They are calculated from CostLineItems and do NOT exist in the database

            // Foreign keys
            costEntity.HasOne(e => e.Document)
                .WithMany(e => e.DependentCosts)
                .HasForeignKey(e => e.IDDokument)
                .OnDelete(DeleteBehavior.Cascade);

            costEntity.HasMany(e => e.CostLineItems)
                .WithOne(e => e.DocumentCost)
                .HasForeignKey(e => e.IDDokumentTroskovi)
                .OnDelete(DeleteBehavior.Cascade);

            // ═══════════════════════════════════════════════════════════════
            // DOCUMENT COST LINE ITEM CONFIGURATION - CRITICAL FOR CONCURRENCY
            // ═══════════════════════════════════════════════════════════════
            var costLineItemEntity = modelBuilder.Entity<DocumentCostLineItem>();
            costLineItemEntity.HasKey(e => e.IDDokumentTroskoviStavka);
            costLineItemEntity.ToTable("tblDokumentTroskoviStavka");

            // RowVersion for concurrency - MANDATORY!
            costLineItemEntity.Property(e => e.DokumentTroskoviStavkaTimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Money types
            costLineItemEntity.Property(e => e.Iznos)
                .HasColumnType("money")
                .HasPrecision(19, 4);

            costLineItemEntity.Property(e => e.Kolicina)
                .HasColumnType("money")
                .HasPrecision(19, 4);

            // Foreign keys
            costLineItemEntity.HasOne(e => e.DocumentCost)
                .WithMany(e => e.CostLineItems)
                .HasForeignKey(e => e.IDDokumentTroskovi)
                .OnDelete(DeleteBehavior.Cascade);

            costLineItemEntity.HasMany(e => e.VATItems)
                .WithOne(e => e.DocumentCostLineItem)
                .HasForeignKey(e => e.IDDokumentTroskoviStavka)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // ═══════════════════════════════════════════════════════════════
            // DEPENDENT COST LINE ITEM CONFIGURATION
            // ═══════════════════════════════════════════════════════════════
            var dependentCostLineItemEntity = modelBuilder.Entity<DependentCostLineItem>();

            dependentCostLineItemEntity.Property(e => e.Amount)
                .HasColumnType("decimal(19, 4)")
                .HasPrecision(19, 4);

            // ═══════════════════════════════════════════════════════════════
            // API AUDIT LOG CONFIGURATION
            // ═══════════════════════════════════════════════════════════════
            var auditLogEntity = modelBuilder.Entity<ApiAuditLog>();
            auditLogEntity.HasKey(e => e.IDAuditLog);
            auditLogEntity.ToTable("tblAPIAuditLog");

            auditLogEntity.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            auditLogEntity.Property(e => e.HttpMethod)
                .HasMaxLength(10)
                .IsRequired();

            auditLogEntity.Property(e => e.Endpoint)
                .HasMaxLength(500)
                .IsRequired();

            auditLogEntity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsRequired();

            auditLogEntity.Property(e => e.IsSuccess)
                .IsRequired()
                .HasDefaultValue(true);

            // Relationships
            auditLogEntity.HasMany(e => e.EntityChanges)
                .WithOne(e => e.AuditLog)
                .HasForeignKey(e => e.IDAuditLog)
                .OnDelete(DeleteBehavior.Cascade);

            // API AUDIT LOG ENTITY CHANGE CONFIGURATION
            var auditChangeEntity = modelBuilder.Entity<ApiAuditLogEntityChange>();
            auditChangeEntity.HasKey(e => e.IDEntityChange);
            auditChangeEntity.ToTable("tblAPIAuditLogEntityChanges");

            auditChangeEntity.Property(e => e.PropertyName)
                .HasMaxLength(100)
                .IsRequired();

            base.OnModelCreating(modelBuilder);
        }
    }
}
