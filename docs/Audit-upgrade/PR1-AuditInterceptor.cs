using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERPAccounting.Application.Common.Interfaces;
using ERPAccounting.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERPAccounting.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// Interceptor koji automatski popunjava audit property-je pri SaveChanges().
    /// Implementira soft delete pattern - DELETE operacije se konvertuju u UPDATE sa IsDeleted = true.
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;

        public AuditInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Ažurira audit property-je za sve izmenjene entitete.
        /// </summary>
        private void UpdateEntities(DbContext context)
        {
            if (context == null) return;

            var entries = context.ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.State == EntityState.Added || 
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();

            var timestamp = DateTime.UtcNow;
            var username = _currentUserService.Username;

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        HandleAddedEntity(entry.Entity, timestamp, username);
                        break;

                    case EntityState.Modified:
                        HandleModifiedEntity(entry.Entity, timestamp, username);
                        break;

                    case EntityState.Deleted:
                        HandleDeletedEntity(entry, timestamp, username);
                        break;
                }
            }
        }

        /// <summary>
        /// Popunjava audit polja za novi entitet.
        /// </summary>
        private void HandleAddedEntity(BaseEntity entity, DateTime timestamp, string username)
        {
            entity.CreatedAt = timestamp;
            entity.CreatedBy = username;
            entity.IsDeleted = false;
            entity.UpdatedAt = timestamp;
            entity.UpdatedBy = username;
        }

        /// <summary>
        /// Ažurira audit polja za izmenjeni entitet.
        /// VAŽNO: NE menjamo CreatedAt i CreatedBy - oni se postavljaju samo pri kreiranju.
        /// </summary>
        private void HandleModifiedEntity(BaseEntity entity, DateTime timestamp, string username)
        {
            entity.UpdatedAt = timestamp;
            entity.UpdatedBy = username;
        }

        /// <summary>
        /// Implementira SOFT DELETE pattern.
        /// Umesto fizičkog brisanja, menja State u Modified i setuje IsDeleted = true.
        /// EF Core će generisati UPDATE umesto DELETE statement.
        /// </summary>
        private void HandleDeletedEntity(EntityEntry<BaseEntity> entry, DateTime timestamp, string username)
        {
            // KLJUČNO: Menjamo state iz Deleted u Modified
            entry.State = EntityState.Modified;
            
            entry.Entity.IsDeleted = true;
            entry.Entity.UpdatedAt = timestamp;
            entry.Entity.UpdatedBy = username;
        }
    }
}

// LOKACIJA: src/ERPAccounting.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs
// TIP: NOVI FAJL