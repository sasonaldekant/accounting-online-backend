# Development Tools

## JWT Token Generator

### Metoda 1: Koristeći C# skript (preporuka)

Instaliraj `dotnet-script` ako ga nemaš:

```powershell
dotnet tool install -g dotnet-script
```

Generiši token:

```powershell
dotnet script tools/GenerateJwtToken.csx
```

Token će biti prikazan u konzoli i možeš ga direktno kopirati za korišćenje u Swaggeru.

### Metoda 2: Koristeći `dotnet user-jwts`

Iz root-a projekta:

```powershell
dotnet user-jwts create --project src/ERPAccounting.API `
  --name "test-admin" `
  --issuer "https://localhost:7280" `
  --audience "https://localhost:7280" `
  --claim "sub=test-user-123" `
  --claim "role=Admin" `
  --claim "email=test@example.com"
```

### Korišćenje tokena u Swaggeru

1. Pokreni API:
   ```powershell
   dotnet run --project src/ERPAccounting.API --launch-profile https
   ```

2. Otvori browser na `https://localhost:7280/swagger`

3. Klikni na zeleni **"Authorize"** dugme (gornji desni ugao)

4. Unesi token u formatu:
   ```
   Bearer <tvoj_generisani_token>
   ```

5. Klikni **Authorize**, zatim **Close**

6. Svi endpoint pozivi će sada automatski koristiti token

### Provera tokena

Token možeš dekodirati i proveriti na: https://jwt.io

### Parametri JWT konfiguracije

Svi parametri se nalaze u `src/ERPAccounting.API/appsettings.Development.json`:

- **Issuer**: `https://localhost:7280`
- **Audience**: `https://localhost:7280`
- **SigningKey**: Mora biti minimum 32 karaktera

**Napomena**: Nikada ne komituj `appsettings.Development.json` sa pravim production podacima!
