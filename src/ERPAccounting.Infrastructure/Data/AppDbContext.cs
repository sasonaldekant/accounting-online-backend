using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    /// AUDIT SYSTEM (NOVI PRISTUP):
    /// - NE menjamo postojeće entitete (Document, DocumentLineItem, itd.)
    /// - Koristimo EF ChangeTracker za izvlačenje JSON snapshots
    /// - Čuvamo kompletno stanje u tblAPIAuditLogEntityChanges
    /// - Akcija se zaključuje iz HTTP metode (POST=Insert, PUT=Update, DELETE=Delete)
    /// - JSON se čuva u OldValue/NewValue kolonama
    /// 
    /// TRIGGER COMPATIBILITY:
    /// - All transactional tables have database triggers for auditing/history
    /// - HasTrigger() is configured to prevent OUTPUT clause usage
    /// - This ensures compatibility with SQL Server triggers on INSERT operations
    /// </summary>
    public class AppDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditLogService? _auditLogService;
        private int? _currentAuditLogId;
        
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ICurrentUserService currentUserService,
            IAuditLogService? auditLogService = null) : base(options)
        {
            _currentUserService = currentUserService;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Postavlja trenutni audit log ID za ovaj request.
        /// Middleware poziva ovo sa IDAuditLog-om da bi SaveChanges mogao da povezuje izmene.
        /// </summary>
        public void SetCurrentAuditLogId(int auditLogId)
        {
            _currentAuditLogId = auditLogId;
        }

        /// <summary>
        /// Override SaveChangesAsync sa automatskim JSON snapshot tracking-om.
        /// Ako je _currentAuditLogId setuvan, sve izmene na entitetima će biti logovane kao JSON snapshots.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Ako imamo audit log ID, prikupi JSON snapshots PRE save-a
            List<(string EntityType, string EntityId, string Operation, object? OldState, object? NewState)>? snapshots = null;
            
            if (_currentAuditLogId.HasValue && _auditLogService != null)
            {
                snapshots = CaptureEntitySnapshots();
            }

            // Izvrši glavni save
            var result = await base.SaveChangesAsync(cancellationToken);

            // Loguj snapshots POSLE save-a (da bi imali ID-eve novih entiteta)
            if (snapshots != null && snapshots.Any())
            {
                await LogCapturedSnapshotsAsync(snapshots);
            }

            // Reset audit log ID
            _currentAuditLogId = null;

            return result;
        }

        /// <summary>
        /// NOVA METODA: Prikuplja kompletne JSON snapshots svih promenjenih entiteta.
        /// Koristi ChangeTracker za izvlačenje stanja pre i posle izmene.
        /// </summary>
        private List<(string EntityType, string EntityId, string Operation, object? OldState, object? NewState)> CaptureEntitySnapshots()
        {
            var snapshots = new List<(string, string, string, object?, object?)>();

            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || 
                           e.State == EntityState.Modified || 
                           e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                // Preskočemo same audit tabele (ne auditujemo audit)
                if (entry.Entity is ApiAuditLog or ApiAuditLogEntityChange)
                    continue;

                var entityType = entry.Entity.GetType().Name;
                var primaryKey = GetPrimaryKeyValue(entry);
                var operation = entry.State.ToString();

                object? oldState = null;
                object? newState = null;

                try
                {
                    if (entry.State == EntityState.Added)
                    {
                        // Za Added - samo novo stanje
                        newState = CreateSnapshot(entry, useCurrentValues: true);
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        // Za Modified - oba stanja
                        oldState = CreateSnapshot(entry, useCurrentValues: false);
                        newState = CreateSnapshot(entry, useCurrentValues: true);
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        // Za Deleted - samo staro stanje
                        oldState = CreateSnapshot(entry, useCurrentValues: false);
                    }

                    snapshots.Add((entityType, primaryKey, operation, oldState, newState));
                }
                catch (Exception ex)
                {
                    // Ne dozvoljavamo da greška u snapshot-u prekine save
                    // Logger je već pozvan, samo nastavljamo
                    System.Diagnostics.Debug.WriteLine($"Failed to capture snapshot for {entityType}:{primaryKey} - {ex.Message}");
                }
            }

            return snapshots;
        }

        /// <summary>
        /// Kreira snapshot objekta iz EntityEntry-ja.
        /// </summary>
        private Dictionary<string, object?> CreateSnapshot(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, bool useCurrentValues)
        {
            var snapshot = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (ShouldIncludeInSnapshot(property))
                {
                    var value = useCurrentValues ? property.CurrentValue : property.OriginalValue;
                    snapshot[property.Metadata.Name] = value;
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Loguje prikupljene JSON snapshots u audit log.
        /// </summary>
        private async Task LogCapturedSnapshotsAsync(
            List<(string EntityType, string EntityId, string Operation, object? OldState, object? NewState)> snapshots)
        {
            if (_auditLogService == null || !_currentAuditLogId.HasValue)
                return;

            foreach (var snapshot in snapshots)
            {
                try
                {
                    // Loguj kompletan JSON snapshot
                    await _auditLogService.LogEntitySnapshotAsync(
                        _currentAuditLogId.Value,
                        snapshot.EntityType,
                        snapshot.EntityId,
                        snapshot.Operation,
                        snapshot.OldState,
                        snapshot.NewState
                    );
                }
                catch
                {
                    // Ignore audit failures - ne smeju da prekinu main transaction
                    // Greška je već logovana u AuditLogService
                }
            }
        }

        /// <summary>
        /// Da li treba uključiti ovu property u snapshot.
        /// </summary>
        private bool ShouldIncludeInSnapshot(Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry property)
        {
            var propertyName = property.Metadata.Name;

            // Ne uključuj RowVersion/TimeStamp kolone (binarne vrednosti)
            if (propertyName.EndsWith("TimeStamp") || propertyName.Contains("RowVersion"))
                return false;

            // Ne uključuj internal EF tracking properties
            if (propertyName.StartsWith("__"))
                return false;

            // Ne uključuj navigation properties (one nisu skladištene u bazi)
            if (property.Metadata.IsNavigation())
                return false;

            return true;
        }

        /// <summary>
        /// Izvlači vrednost primarnog ključa iz entity-ja.
        /// </summary>
        private string GetPrimaryKeyValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            return keyProperty?.CurrentValue?.ToString() ?? "UNKNOWN";
        }

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
        // AUDIT LOG TABLES (tables for tracking changes with JSON snapshots)
        // ═══════════════════════════════════════════════════════════════
        public DbSet<ApiAuditLog> ApiAuditLogs { get; set; } = null!;
        public DbSet<ApiAuditLogEntityChange> ApiAuditLogEntityChanges { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // NOTE: Global query filter for ISoftDeletable has been REMOVED.
            // Soft delete is now tracked via ApiAuditLog tables with JSON snapshots.
            // This prevents "Invalid column name 'IsDeleted'" SQL exceptions.

            // ═══════════════════════════════════════════════════════════════
            // DOCUMENT CONFIGURATION
            // ═══════════════════════════════════════════════════════════════
            var documentEntity = modelBuilder.Entity<Document>();
            documentEntity.HasKey(e => e.IDDokument);
            
            // CRITICAL: Configure trigger to prevent OUTPUT clause usage
            documentEntity.ToTable("tblDokument", t => t.HasTrigger("TR_tblDokument_Insert"));

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
            
            // CRITICAL: Configure trigger to prevent OUTPUT clause usage
            lineItemEntity.ToTable("tblStavkaDokumenta", t => t.HasTrigger("TR_tblStavkaDokumenta_Insert"));

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
            
            // CRITICAL: Configure trigger to prevent OUTPUT clause usage
            costEntity.ToTable("tblDokumentTroskovi", t => t.HasTrigger("TR_tblDokumentTroskovi_Insert"));

            // RowVersion for concurrency
            costEntity.Property(e => e.DokumentTroskoviTimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Kurs column configuration
            costEntity.Property(e => e.Kurs)
                .HasColumnType("money")
                .HasPrecision(19, 4);

            // NOTE: IznosBezPDV and IznosPDV are [NotMapped] computed properties

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
            
            // CRITICAL: Configure trigger to prevent OUTPUT clause usage
            costLineItemEntity.ToTable("tblDokumentTroskoviStavka", t => t.HasTrigger("TR_tblDokumentTroskoviStavka_Insert"));

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
            // DOCUMENT COST VAT CONFIGURATION
            // ═══════════════════════════════════════════════════════════════
            var costVATEntity = modelBuilder.Entity<DocumentCostVAT>();
            costVATEntity.HasKey(e => e.IDDokumentTroskoviStavkaPDV);
            
            // CRITICAL: Configure trigger to prevent OUTPUT clause usage
            costVATEntity.ToTable("tblDokumentTroskoviStavkaPDV", t => t.HasTrigger("TR_tblDokumentTroskoviStavkaPDV_Insert"));

            // ═══════════════════════════════════════════════════════════════
            // DOCUMENT ADVANCE VAT CONFIGURATION
            // ═══════════════════════════════════════════════════════════════
            var advanceVATEntity = modelBuilder.Entity<DocumentAdvanceVAT>();
            advanceVATEntity.HasKey(e => e.DokumentAvansPDV);
            
            // CRITICAL: Configure trigger to prevent OUTPUT clause usage
            advanceVATEntity.ToTable("tblDokumentAvansPDV", t => t.HasTrigger("TR_tblDokumentAvansPDV_Insert"));

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