using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Infrastructure.Persistence.Interceptors;

namespace ERPAccounting.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core DbContext za ERP Accounting sistem
    /// Mapira sve tabele i konfigurira relacije
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Registruj AuditInterceptor
            optionsBuilder.AddInterceptors(new AuditInterceptor(_currentUserService));
            
            base.OnConfiguring(optionsBuilder);
        }

        // ═══════════════════════════════════════════════════════════════
        // GLAVNE TABELE
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<DocumentLineItem> DocumentLineItems { get; set; } = null!;
        public DbSet<DocumentCost> DocumentCosts { get; set; } = null!;
        public DbSet<DocumentCostLineItem> DocumentCostLineItems { get; set; } = null!;
        public DbSet<DocumentAdvanceVAT> DocumentAdvanceVATs { get; set; } = null!;
        public DbSet<DependentCostLineItem> DependentCostLineItems { get; set; } = null!;
        public DbSet<DocumentCostVAT> DocumentCostVATs { get; set; } = null!;

        // ═══════════════════════════════════════════════════════════════
        // AUDIT LOG TABELE
        public DbSet<ApiAuditLog> ApiAuditLogs { get; set; } = null!;
        public DbSet<ApiAuditLogEntityChange> ApiAuditLogEntityChanges { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply configurations
            // (ako postoje Configuration klase u projektu, možete koristiti ApplyConfigurationsFromAssembly)
            // modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // DODAJ: Global query filter za soft delete
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                    var filter = Expression.Lambda(Expression.Not(property), parameter);
                    
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // DOCUMENT KONFIGURACIJA
            var documentEntity = modelBuilder.Entity<Document>();
            documentEntity.HasKey(e => e.IDDokument);
            documentEntity.ToTable("tblDokument");

            // RowVersion za konkurentnost - OBAVEZNO!
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

            modelBuilder.Entity<DocumentAdvanceVAT>()
                .HasQueryFilter(e => !e.Document.IsDeleted);

            // ═══════════════════════════════════════════════════════════════
            // DOCUMENT LINE ITEM KONFIGURACIJA - KRITIČNO ZA KONKURENTNOST
            var lineItemEntity = modelBuilder.Entity<DocumentLineItem>();
            lineItemEntity.HasKey(e => e.IDStavkaDokumenta);
            lineItemEntity.ToTable("tblStavkaDokumenta");

            // RowVersion za konkurentnost - OBAVEZNO!
            lineItemEntity.Property(e => e.StavkaDokumentaTimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Money tipovi sa tačnom preciznostišću
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
            // DOCUMENT COST KONFIGURACIJA
            var costEntity = modelBuilder.Entity<DocumentCost>();
            costEntity.HasKey(e => e.IDDokumentTroskovi);
            costEntity.ToTable("tblDokumentTroskovi");

            // RowVersion za konkurentnost
            costEntity.Property(e => e.DokumentTroskoviTimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

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
            // DOCUMENT COST LINE ITEM KONFIGURACIJA - KRITIČNO ZA KONKURENTNOST
            var costLineItemEntity = modelBuilder.Entity<DocumentCostLineItem>();
            costLineItemEntity.HasKey(e => e.IDDokumentTroskoviStavka);
            costLineItemEntity.ToTable("tblDokumentTroskoviStavka");

            // RowVersion za konkurentnost - OBAVEZNO!
            costLineItemEntity.Property(e => e.DokumentTroskoviStavkaTimeStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Money tipovi
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

            modelBuilder.Entity<DocumentCostVAT>()
                .HasQueryFilter(e => !e.DocumentCostLineItem.IsDeleted);

            // ═══════════════════════════════════════════════════════════════
            // DEPENDENT COST LINE ITEM KONFIGURACIJA
            var dependentCostLineItemEntity = modelBuilder.Entity<DependentCostLineItem>();

            dependentCostLineItemEntity.Property(e => e.Amount)
                .HasColumnType("decimal(19, 4)")
                .HasPrecision(19, 4);

            // ═══════════════════════════════════════════════════════════════
            // API AUDIT LOG KONFIGURACIJA
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

            // API AUDIT LOG ENTITY CHANGE KONFIGURACIJA
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