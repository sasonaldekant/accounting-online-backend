# 🔒 ETag & Concurrency Control - Kompletan Vodič

## 🎯 Pregled

**ETag (Entity Tag)** sistem omogućava **optimistic concurrency control** — sprečava "lost update" scenario kada više korisnika menja isti dokument istovremeno.

### ✅ Zašto Je Bitan?

**Scenario bez ETag-a:**
```
1. Korisnik A očitava dokument #123 (Iznos: 1000€)
2. Korisnik B očitava dokument #123 (Iznos: 1000€)
3. Korisnik A menja iznos na 1500€ i save-uje ✅
4. Korisnik B menja iznos na 2000€ i save-uje ✅
5. Rezultat: 2000€ — Promena korisnika A je IZGUBLJENA! ❌
```

**Scenario sa ETag-om:**
```
1. Korisnik A očitava dokument #123 (Iznos: 1000€, ETag: "abc123")
2. Korisnik B očitava dokument #123 (Iznos: 1000€, ETag: "abc123")
3. Korisnik A menja iznos na 1500€, šalje If-Match: "abc123" ✅ (uspešno, novi ETag: "def456")
4. Korisnik B menja iznos na 2000€, šalje If-Match: "abc123" ❌ (409 Conflict!)
5. Korisnik B dobija poruku: "Dokument je promenjen, refresh i pokušaj ponovo"
6. Korisnik B refresh-uje, vidi 1500€, ponovo unosi 2000€ sa novim ETag-om ✅
```

---

## 🛠️ Kako Radi

### 1. Backend Implementacija

#### a) RowVersion Polja

Svaki kritiči entitet ima **rowversion** polje:

```csharp
// Document.cs
public class Document
{
    public int IDDokument { get; set; }
    public string BrojDokumenta { get; set; }
    
    // RowVersion za concurrency
    [Timestamp]
    public byte[]? DokumentTimeStamp { get; set; }
}
```

#### b) EF Core Konfiguracija

```csharp
// AppDbContext.cs
modelBuilder.Entity<Document>()
    .Property(e => e.DokumentTimeStamp)
    .IsRowVersion()
    .IsConcurrencyToken();
```

Ovo čini da EF automatski:
- Generiše `WHERE DokumentTimeStamp = @p0` u UPDATE statement-u
- Baca `DbUpdateConcurrencyException` ako rowversion ne odgovara

#### c) DTO Konverzija

```csharp
// DocumentDto.cs
public class DocumentDto
{
    public int Id { get; set; }
    public string BrojDokumenta { get; set; }
    
    // ETag kao Base64 string
    public string? ETag { get; set; }
}

// Mapping
public DocumentDto MapToDto(Document entity)
{
    return new DocumentDto
    {
        Id = entity.IDDokument,
        BrojDokumenta = entity.BrojDokumenta,
        ETag = entity.DokumentTimeStamp != null 
            ? Convert.ToBase64String(entity.DokumentTimeStamp) 
            : null
    };
}
```

### 2. API Endpoints

#### GET - Vraća ETag u Header-u

```http
GET /api/v1/documents/123
Authorization: Bearer {token}
```

**Response:**
```http
HTTP/1.1 200 OK
ETag: "AAAAAAAAB9Q="
Content-Type: application/json

{
  "id": 123,
  "brojDokumenta": "DOK-001",
  "iznos": 1000.00,
  "etag": "AAAAAAAAB9Q="
}
```

#### PUT/PATCH - Zahteva If-Match Header

```http
PATCH /api/v1/documents/123
Authorization: Bearer {token}
If-Match: "AAAAAAAAB9Q="
Content-Type: application/json

{
  "iznos": 1500.00
}
```

**Success Response:**
```http
HTTP/1.1 200 OK
ETag: "AAAAAAAAB+A="
Content-Type: application/json

{
  "id": 123,
  "brojDokumenta": "DOK-001",
  "iznos": 1500.00,
  "etag": "AAAAAAAAB+A="
}
```

**Conflict Response (409):**
```http
HTTP/1.1 409 Conflict
ETag: "AAAAAAAAB+A="
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Concurrency Conflict",
  "status": 409,
  "detail": "Document has been modified by another user",
  "instance": "/api/v1/documents/123",
  "traceId": "00-abc123...",
  "errors": {
    "concurrency": [
      "Entity 'Document' has been modified by another user.",
      "Expected ETag: AAAAAAAAB9Q=",
      "Current ETag: AAAAAAAAB+A=",
      "Please refresh the entity and try again."
    ]
  },
  "entityType": "Document",
  "expectedETag": "AAAAAAAAB9Q=",
  "currentETag": "AAAAAAAAB+A=",
  "errorCode": "CONCURRENCY_CONFLICT"
}
```

---

## 💻 Frontend Implementacija

### JavaScript/TypeScript Primer

```typescript
interface Document {
  id: number;
  brojDokumenta: string;
  iznos: number;
  etag: string;
}

class DocumentService {
  async getDocument(id: number): Promise<{ data: Document; etag: string }> {
    const response = await fetch(`/api/v1/documents/${id}`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });
    
    const data = await response.json();
    const etag = response.headers.get('ETag')?.replace(/"/g, '') || data.etag;
    
    return { data, etag };
  }
  
  async updateDocument(
    id: number, 
    changes: Partial<Document>, 
    etag: string
  ): Promise<Document> {
    const response = await fetch(`/api/v1/documents/${id}`, {
      method: 'PATCH',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
        'If-Match': `"${etag}"` // BITNO: sa navodnicima
      },
      body: JSON.stringify(changes)
    });
    
    if (response.status === 409) {
      const problem = await response.json();
      throw new ConcurrencyError(
        'Document has been modified by another user',
        problem.currentETag
      );
    }
    
    return response.json();
  }
}

// Retry logika
async function updateWithRetry(id: number, changes: Partial<Document>) {
  let retries = 3;
  
  while (retries > 0) {
    try {
      // Očitaj trenutno stanje
      const { data, etag } = await documentService.getDocument(id);
      
      // Primeni izmene
      const updated = await documentService.updateDocument(id, changes, etag);
      
      console.log('Successfully updated:', updated);
      return updated;
      
    } catch (error) {
      if (error instanceof ConcurrencyError) {
        console.warn('Concurrency conflict, retrying...', error);
        retries--;
        
        if (retries === 0) {
          // Poslednji pokušaj propao - obavesti korisnika
          alert('Dokument je promenjen više puta. Molimo osvežite stranicu.');
          throw error;
        }
        
        // Retry sa novim ETag-om
        continue;
      }
      
      // Druga greška
      throw error;
    }
  }
}
```

### React Hook Primer

```typescript
function useDocumentWithConcurrency(id: number) {
  const [document, setDocument] = useState<Document | null>(null);
  const [etag, setETag] = useState<string>('');
  const [conflictError, setConflictError] = useState<string | null>(null);
  
  const fetchDocument = async () => {
    const { data, etag } = await documentService.getDocument(id);
    setDocument(data);
    setETag(etag);
    setConflictError(null);
  };
  
  const updateDocument = async (changes: Partial<Document>) => {
    try {
      const updated = await documentService.updateDocument(id, changes, etag);
      setDocument(updated);
      setETag(updated.etag);
      setConflictError(null);
      return true;
    } catch (error) {
      if (error instanceof ConcurrencyError) {
        setConflictError(
          'Dokument je promenjen od strane drugog korisnika. ' +
          'Kliknite "Osveži" da vidite najnovije izmene.'
        );
        return false;
      }
      throw error;
    }
  };
  
  const refreshAndRetry = async (changes: Partial<Document>) => {
    await fetchDocument(); // Osvež
    return updateDocument(changes); // Pokušaj ponovo
  };
  
  return { document, updateDocument, conflictError, refreshAndRetry };
}
```

---

## 🛡️ Backend Best Practices

### 1. Automatski ETag Filter

**Registruj filter globalno:**

```csharp
// Program.cs
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ETagFilter>(); // Automatski setuje ETag header
    options.Filters.Add<ConcurrencyExceptionFilter>(); // Standardizovani 409 response
});
```

**Rezultat:** Više ne moraš ručno pisati `Response.Headers["ETag"] = ...` u svakom controller-u!

### 2. Consistency - Svi Update Endpoint-i

SVAKI update endpoint (PUT/PATCH) MORA:
- Prihvatiti `If-Match` header
- Validirati protiv trenutnog rowversion-a
- Baciti `ConflictException` na neslaganje
- Vratiti novi ETag u response-u

### 3. Exception Handling

```csharp
try
{
    var updated = await _service.UpdateAsync(id, expectedRowVersion, dto);
    return Ok(updated); // ETagFilter automatski dodaje header
}
catch (ConflictException ex)
{
    // ConcurrencyExceptionFilter automatski hendluje
    throw;
}
```

### 4. Testing

```csharp
[Fact]
public async Task UpdateDocument_ConcurrentModification_Returns409()
{
    // Arrange
    var document = await CreateTestDocument();
    var originalETag = document.ETag;
    
    // Act: Prva izmena
    var firstUpdate = await _client.PatchAsync(
        $"/api/v1/documents/{document.Id}",
        JsonContent(new { Iznos = 1500 }),
        headers: new { ["If-Match"] = $"\"{originalETag}\"" }
    );
    firstUpdate.EnsureSuccessStatusCode();
    
    // Act: Druga izmena sa STARIM ETag-om
    var secondUpdate = await _client.PatchAsync(
        $"/api/v1/documents/{document.Id}",
        JsonContent(new { Iznos = 2000 }),
        headers: new { ["If-Match"] = $"\"{originalETag}\"" } // Stari ETag!
    );
    
    // Assert
    Assert.Equal(HttpStatusCode.Conflict, secondUpdate.StatusCode);
    
    var problem = await secondUpdate.Content.ReadAsAsync<ProblemDetailsDto>();
    Assert.Equal("CONCURRENCY_CONFLICT", problem.Extensions["errorCode"]);
    Assert.NotEqual(originalETag, problem.Extensions["currentETag"]);
}
```

---

## 🐛 Common Pitfalls

### ❌ **1. Zaboravljeni navodnivi**

```http
❌ If-Match: AAAAAAAAB9Q=
✅ If-Match: "AAAAAAAAB9Q="
```

ETag MORA biti u navodnicima (RFC 7232).

### ❌ **2. Case Sensitivity**

```http
❌ if-match: "..."
✅ If-Match: "..."
```

HTTP headers su case-insensitive, ali konvencija je `Title-Case`.

### ❌ **3. Nečitanje ETag-a iz Response-a**

```typescript
❌ const etag = data.etag; // Samo iz body-ja
✅ const etag = response.headers.get('ETag') || data.etag; // Header ima prioritet
```

### ❌ **4. Ignorišanje 409 Conflict**

```typescript
❌ if (response.status === 409) { /* ignore */ }
✅ if (response.status === 409) { 
     await refreshAndRetry(); 
   }
```

---

## 📊 Monitoring & Metrics

### SQL Query za Tracking Concurrency Failures

```sql
-- Broj 409 Conflict response-ova u poslednjih 24h
SELECT 
    COUNT(*) AS ConflictCount,
    Endpoint,
    Username
FROM tblAPIAuditLog
WHERE ResponseStatusCode = 409
  AND Timestamp > DATEADD(HOUR, -24, GETUTCDATE())
GROUP BY Endpoint, Username
ORDER BY ConflictCount DESC;
```

### Application Insights / Logging

```csharp
_logger.LogWarning(
    "Concurrency conflict: User={User}, Entity={Entity}, Expected={Expected}, Current={Current}",
    username, entityType, expectedETag, currentETag
);
```

---

## ✅ Checklist

### Backend
- [x] Svi kritiči entiteti imaju `[Timestamp]` property
- [x] EF konfiguracija sa `.IsRowVersion().IsConcurrencyToken()`
- [x] DTO-ovi imaju `ETag` property (Base64 string)
- [x] `IfMatchHeaderParser` u svim update endpoint-ima
- [x] `ETagFilter` registrovan globalno
- [x] `ConcurrencyExceptionFilter` registrovan globalno
- [x] Unit testovi za concurrent updates

### Frontend
- [ ] Čuvanje ETag-a iz GET response-a
- [ ] Slanje `If-Match` header-a u svim update zahtevima
- [ ] Handling 409 Conflict sa retry logikom
- [ ] User-friendly poruke za concurrency konflikte
- [ ] Refresh dugme posle konflikta

### Dokumentacija
- [x] API dokumentacija sa ETag primerom
- [x] Frontend guide za ETag handling
- [ ] Swagger annotations sa If-Match/ETag header-ima

---

## 🚀 Production Considerations

1. **Rate Limiting** - Limitiraj broj retry-a (max 3)
2. **User Feedback** - Jasne poruke: "Dokument promenjen, kliknite Osveži"
3. **Analytics** - Prati koliko često se dešavaju konflikti po endpoint-u
4. **Performance** - ETag ne utiče na performanse (rowversion je u index-u)
5. **Caching** - Iskoristi ETag za HTTP caching (`304 Not Modified`)

---

## 📚 Reference

- [RFC 7232 - HTTP/1.1: Conditional Requests](https://tools.ietf.org/html/rfc7232)
- [EF Core Concurrency Tokens](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Optimistic vs Pessimistic Locking](https://en.wikipedia.org/wiki/Optimistic_concurrency_control)

---

**Za pitanja i problemem, pregledaj `docs/TROUBLESHOOTING.md` ili otvori issue!**
