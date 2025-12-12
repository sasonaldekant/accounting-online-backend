# 🔴 DATETIME CONVERTER - FINAL DEBUG GUIDE

**Current Error**: Line 264 in `IsoNullableDateTimeConverter.Read()`  
**Error Message**: `The DateTimeStyles value RoundtripKind cannot be used with the values AssumeLocal, AssumeUniversal or AdjustToUniversal`  

---

## 🔍 ROOT CAUSE ANALYSIS

### Problem: Error is Still on Line 264

```csharp
// Line 264 is in this area (IsoNullableDateTimeConverter.Read):
if (DateTime.TryParse(
    dateString,
    CultureInfo.InvariantCulture,
    DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal,  // ← LINE 264 CONFLICT!
    out DateTime resultAsUtc))
```

This means the code **still has the bitwise OR** between incompatible flags!

### Why This Happens

**Possible Causes**:
1. ❌ Backend was not restarted after code fix
2. ❌ Changes were not committed/pushed
3. ❌ Visual Studio is serving cached version
4. ❌ IIS Express has old assembly cached
5. ❌ Build configuration mismatch

---

## ✅ STEP-BY-STEP FIX

### STEP 1: Clean & Verify Code

```csharp
// ✅ CORRECT CODE (No bitwise OR!)

public class IsoNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        string? dateString = reader.GetString();

        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }

        // ✅ STEP 1: RoundtripKind ALONE (no | operator)
        if (DateTime.TryParse(
            dateString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,    // ← SOLO
            out DateTime result))
        {
            if (result.Kind == DateTimeKind.Local)
            {
                return result.ToUniversalTime();
            }
            return result;
        }

        // ✅ STEP 2: AssumeUniversal ALONE (separate call)
        if (DateTime.TryParse(
            dateString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,  // ← SOLO
            out DateTime resultAsUtc))
        {
            return resultAsUtc;
        }

        // ✅ STEP 3: Default parse
        if (DateTime.TryParse(dateString, out DateTime fallback))
        {
            if (fallback.Kind == DateTimeKind.Local)
            {
                return fallback.ToUniversalTime();
            }
            return fallback;
        }

        return null;  // Safe fallback for nullable
    }

    public override void Write(...) { ... }
}
```

### STEP 2: Complete Backend Cleanup

```powershell
# 1. Stop any running instance
# Visual Studio: Press Ctrl+Shift+B (Stop debugging)
# OR: dotnet build

# 2. Clean build artifacts
cd E:\PROGRAMING\AI Projects\AccountingOnline\accounting-online-backend
dotnet clean
del /s /q bin obj

# 3. Full rebuild
dotnet build --configuration Debug

# 4. Clear Visual Studio cache (if using VS)
# Close Visual Studio completely
# Delete: C:\Users\[username]\AppData\Local\Microsoft\VisualStudio\[version]\ComponentModelCache

# 5. Restart backend
# Visual Studio: F5 to start debugging
# OR: dotnet run
```

### STEP 3: Clear Browser Cache

```javascript
// Browser Console (F12)
// 1. Clear all storage
localStorage.clear();
sessionStorage.clear();

// 2. Clear service workers
navigator.serviceWorker?.getRegistrations()
  .then(registrations => registrations.forEach(r => r.unregister()));

// 3. Hard refresh
// Ctrl+Shift+R (Windows/Linux)
// Cmd+Shift+R (Mac)
```

### STEP 4: Test DateTime Conversion

```javascript
// Browser Console - Test API directly
fetch('http://localhost:5286/api/v1/documents/create', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer ' + localStorage.getItem('token'),
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    documentDate: '2025-12-12T00:00:00.000Z',  // ISO 8601 with timezone
    documentNumber: 'TEST-001',
    // ... other fields
  })
})
.then(r => {
  console.log('Status:', r.status);
  return r.json();
})
.then(data => console.log('Response:', data))
.catch(err => console.error('Error:', err.message));
```

### STEP 5: Verify in Code

Open `Program.cs` and search for:

```csharp
// ❌ WRONG - Still has bitwise OR
DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal

// ✅ CORRECT - No bitwise OR
DateTimeStyles.RoundtripKind,
// ... separate if statement
DateTimeStyles.AssumeUniversal,
```

---

## 🧪 VERIFICATION TESTS

### Test 1: Direct API Call

```bash
# PowerShell
$token = "eyJhbGc..." # Your JWT token

$body = @{
    documentDate = "2025-12-12T00:00:00.000Z"
    documentNumber = "TEST-001"
    warehouseId = 1
    currencyId = 1
    employeeId = 1
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Invoke-RestMethod -Uri "http://localhost:5286/api/v1/documents" `
  -Method Post `
  -Headers $headers `
  -Body $body

# Should return: 201 Created (not 500 with ArgumentException)
```

### Test 2: DateTime Parsing

```csharp
// Add this to Program.cs temporarily (for debugging)
var testDate = "2025-12-12T00:00:00.000Z";

// This should NOT throw exception
var parsed = new IsoNullableDateTimeConverter().Read(
    ref reader,  // mock
    typeof(DateTime?),
    new JsonSerializerOptions()
);

Console.WriteLine($"✅ Parsed: {parsed}");
```

### Test 3: Full Document Creation Flow

```bash
1. Frontend: http://localhost:3000/documents/vp/ur
2. Fill form:
   - Broj: TEST-001
   - Datum: 2025-12-12
   - Magacin: Any
   - Valuta: RSD
   - Zaposlenik: Any
   - Oporezivanje: Any
3. Click: Sačuvaj Dokument
4. Check:
   - Browser Console: No errors
   - Network tab: Status 201 (not 500)
   - Backend Console: No exception
   - Database: Document created with correct date
```

---

## 🚨 IF ERROR STILL PERSISTS

### Scenario 1: Code in file is correct but backend throws error

**Cause**: Visual Studio is serving old compiled assembly  
**Solution**:
```powershell
# Close Visual Studio completely
Get-Process devenv | Stop-Process -Force
Get-Process VsHub | Stop-Process -Force 2>$null

# Delete all build artifacts
rm -r "E:\PROGRAMING\AI Projects\AccountingOnline\accounting-online-backend\bin"
rm -r "E:\PROGRAMING\AI Projects\AccountingOnline\accounting-online-backend\obj"

# Reopen and rebuild
# Visual Studio: Open solution → Rebuild All (Ctrl+Alt+B)
```

### Scenario 2: Code still has old logic

**Check**: Open Program.cs and search for:
```
RoundtripKind |
```

**If found**: Replace with correct code shown in STEP 1 above

### Scenario 3: Multiple DateTime converters registered

**Check Program.cs**:
```csharp
// Should only have these 2:
options.JsonSerializerOptions.Converters.Add(new IsoDateTimeConverter());
options.JsonSerializerOptions.Converters.Add(new IsoNullableDateTimeConverter());

// NOT:
options.JsonSerializerOptions.Converters.Add(new JsonConverter<DateTime>());  // ❌ Remove
options.JsonSerializerOptions.Converters.Add(new JsonConverter<DateTime?>());  // ❌ Remove
```

---

## 📋 FINAL CHECKLIST

- [ ] Opened `src/ERPAccounting.API/Program.cs`
- [ ] Verified `IsoDateTimeConverter` has NO bitwise OR (uses separate if statements)
- [ ] Verified `IsoNullableDateTimeConverter` has NO bitwise OR
- [ ] Closed Visual Studio completely
- [ ] Deleted `/bin` and `/obj` directories
- [ ] Reopened solution in Visual Studio
- [ ] Ran: Rebuild All (Ctrl+Alt+B)
- [ ] Started debugging (F5)
- [ ] Hard refreshed browser (Ctrl+Shift+R)
- [ ] Cleared localStorage and sessionStorage
- [ ] Tested document creation
- [ ] Verified status 201 (not 500)
- [ ] Checked database for saved document

---

## ✨ EXPECTED RESULT

```
✅ Frontend sends: "documentDate": "2025-12-12T00:00:00.000Z"
✅ Backend receives: DateTime(2025-12-12 00:00:00 UTC)
✅ Database saves: 2025-12-12 00:00:00
✅ API returns: 201 Created
✅ NO System.ArgumentException error
```

---

**If this doesn't work after all steps, provide:**
1. Screenshot of current code in Program.cs (lines 240-320)
2. Backend console output when you start it
3. Network response when creating document
4. Git status (any uncommitted changes?)
