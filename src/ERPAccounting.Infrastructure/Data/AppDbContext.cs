using Microsoft.EntityFrameworkCore;
using ERPAccounting.Domain.Entities;

namespace ERPAccounting.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core DbContext za ERP Accounting sistem
    /// Mapira sve tabele i konfigurira relacije
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ═══════════════════════════════════════════════════════════════
            // DOCUMENT KONFIGURACIJA
            var documentEntity = modelBuilder.Entity<Document>();
            documentEntity.HasKey(e => e.IDDokument);
            documentEntity.ToTable("tblDokument");

            // Soft delete filter - sve query-je će filtrirati obrisane
            documentEntity.HasQueryFilter(e => !e.IsDeleted);

            documentEntity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETUTCDATE()");

            documentEntity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETUTCDATE()");

            documentEntity.Property(e => e.CreatedBy)
                .HasColumnType("int");

            documentEntity.Property(e => e.UpdatedBy)
                .HasColumnType("int");

            documentEntity.Property(e => e.IsDeleted)
                .HasColumnType("bit")
                .HasDefaultValue(false);

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

            // ═══════════════════════════════════════════════════════════════
            // DOCUMENT LINE ITEM KONFIGURACIJA - KRITIČNO ZA KONKURENTNOST
            var lineItemEntity = modelBuilder.Entity<DocumentLineItem>();
            lineItemEntity.HasKey(e => e.IDStavkaDokumenta);
            lineItemEntity.ToTable("tblStavkaDokumenta");

            // Soft delete filter
            lineItemEntity.HasQueryFilter(e => !e.IsDeleted);

            lineItemEntity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETUTCDATE()");

            lineItemEntity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETUTCDATE()");

            lineItemEntity.Property(e => e.CreatedBy)
                .HasColumnType("int");

            lineItemEntity.Property(e => e.UpdatedBy)
                .HasColumnType("int");

            lineItemEntity.Property(e => e.IsDeleted)
                .HasColumnType("bit")
                .HasDefaultValue(false);

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
        }
    }
}
