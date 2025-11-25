# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed - 2025-11-24

#### 🔴 CRITICAL: Remove IsDeleted and Audit Fields (PR #1)

**Problem:**
- `Invalid column name 'IsDeleted'` SQL exception
- `Invalid column name 'Napomena'` in DocumentCostLineItem
- Query filters attempting to access non-existent database columns
- BaseEntity audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) marked as [NotMapped] but still causing issues

**Root Cause:**
- Entities inherited from `BaseEntity` which had audit tracking fields
- `ISoftDeletable` interface added `IsDeleted` property to entities
- Global query filter in `AppDbContext` tried to filter by `IsDeleted` column
- Database schema does NOT contain these columns - they were never migrated

**Solution:**
1. **Removed `BaseEntity.cs`** - All audit fields deleted
2. **Removed `ISoftDeletable.cs` interface** - Soft delete will be tracked via Audit tables only
3. **Updated all entities:**
   - `Document.cs` - removed `: BaseEntity, ISoftDeletable`, removed `IsDeleted` property
   - `DocumentLineItem.cs` - removed `: BaseEntity, ISoftDeletable`, removed `IsDeleted` property
   - `DocumentCost.cs` - removed `: BaseEntity`, removed audit fields
   - `DocumentCostLineItem.cs` - removed `: BaseEntity`, removed `Napomena` property (does not exist in tblDokumentTroskoviStavka)
   - `DocumentAdvanceVAT.cs` - removed `: BaseEntity`
   - `DependentCostLineItem.cs` - removed `: BaseEntity`
   - `DocumentCostVAT.cs` - removed `: BaseEntity`

4. **Updated `AppDbContext.cs`:**
   - Removed global query filter loop for `ISoftDeletable`
   - Removed `.Property<bool>("IsDeleted")` configuration
   - Removed `.HasQueryFilter()` calls for soft delete

5. **Updated all Repositories:**
   - Removed `.Where(x => !x.IsDeleted)` clauses
   - Removed `.Where(x => x.IsDeleted == false)` clauses

**Impact:**
- ✅ All Swagger endpoints now work without SQL exceptions
- ✅ Entity models map 1:1 to existing database tables
- ✅ No migrations needed - database unchanged
- ✅ Audit trail still works via dedicated `tblAPIAuditLog` and `tblAPIAuditLogEntityChanges` tables

**Migration Path:**
- **Soft Delete:** Tracked via `ApiAuditLogEntityChange` with `ChangeType = 'DELETE'`
- **Audit Fields:** Tracked via `ApiAuditLog.Username`, `ApiAuditLog.Timestamp`
- **Entity State:** Use EF Core `EntityState.Deleted` for soft delete logic in services

**Files Changed:**
```
DELETED:
  src/ERPAccounting.Domain/Entities/BaseEntity.cs
  src/ERPAccounting.Domain/Interfaces/ISoftDeletable.cs

MODIFIED:
  src/ERPAccounting.Domain/Entities/Document.cs
  src/ERPAccounting.Domain/Entities/DocumentLineItem.cs
  src/ERPAccounting.Domain/Entities/DocumentCost.cs
  src/ERPAccounting.Domain/Entities/DocumentCostLineItem.cs
  src/ERPAccounting.Domain/Entities/DocumentAdvanceVAT.cs
  src/ERPAccounting.Domain/Entities/DependentCostLineItem.cs
  src/ERPAccounting.Domain/Entities/DocumentCostVAT.cs
  src/ERPAccounting.Infrastructure/Data/AppDbContext.cs
  src/ERPAccounting.Infrastructure/Repositories/DocumentRepository.cs
  src/ERPAccounting.Infrastructure/Repositories/DocumentLineItemRepository.cs
  src/ERPAccounting.Infrastructure/Repositories/DocumentCostRepository.cs
  src/ERPAccounting.Infrastructure/Repositories/DocumentCostLineItemRepository.cs
```

**Testing:**
- [x] Swagger GET /api/v1/documents - 200 OK
- [x] Swagger GET /api/v1/documents/{id} - 200 OK
- [x] Swagger GET /api/v1/documents/{id}/items - 200 OK
- [x] Swagger GET /api/v1/documents/{id}/costs - 200 OK
- [x] No SQL exceptions in logs
- [x] ETag still works via RowVersion (DokumentTimeStamp, StavkaDokumentaTimeStamp)

**Breaking Changes:**
- ⚠️ Any code that directly accessed `IsDeleted` property must be refactored
- ⚠️ Any code that accessed `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` must use Audit tables

**Database Changes:**
- ✅ **NONE** - This is purely code refactoring to match existing schema

---

## [0.1.0] - 2025-11-20

### Added
- Initial project setup
- Clean Architecture structure
- Entity Framework Core 8.0 configuration
- Basic entity models
- API Controllers scaffolding
- Swagger/OpenAPI documentation

---

## Template for Future Changes

```markdown
## [Version] - YYYY-MM-DD

### Added
- New features

### Changed
- Changes in existing functionality

### Deprecated
- Soon-to-be removed features

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Security improvements
```
