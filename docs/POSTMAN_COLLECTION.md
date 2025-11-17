# 📮 POSTMAN COLLECTION - ERP ACCOUNTING API

**Base URL:** `http://localhost:5000/api/v1`  
**Authorization:** Bearer Token (JWT)

---

## 🔐 Setup

1. **Kreira fajl:** `ERP-Accounting.postman_collection.json`
2. **Import u Postman**
3. **Postavi URL**:
   - Development: `http://localhost:5000`
   - Production: `https://api.accounting.yourdomain.com`

---

## 📌 LOOKUPS ENDPOINTS (11 SP-ova)

### 1. Get Partners
```
GET /api/v1/lookups/partners
Authorization: Bearer {{token}}

Response:
[
  {
    "idPartner": 1,
    "nazivPartnera": "Partner A",
    "mesto": "Beograd",
    "sifra": "P001",
    "status": 1
  }
]
```

### 2. Get Organizational Units
```
GET /api/v1/lookups/organizational-units?docTypeId=UR
Authorization: Bearer {{token}}

Response:
[
  {
    "idOrganizacionaJedinica": 1,
    "naziv": "Glavni ured",
    "mesto": "Beograd",
    "sifra": "OU001"
  }
]
```

### 3. Get Taxation Methods
```
GET /api/v1/lookups/taxation-methods
Authorization: Bearer {{token}}

Response:
[
  {
    "idNacinOporezivanja": 1,
    "opis": "PDV 20%",
    "obracunAkciza": 0,
    "obracunPorez": 1
  }
]
```

### 4. Get Referents
```
GET /api/v1/lookups/referents
Authorization: Bearer {{token}}
```

### 5. Get Documents ND
```
GET /api/v1/lookups/documents-nd
Authorization: Bearer {{token}}
```

### 6. Get Tax Rates
```
GET /api/v1/lookups/tax-rates
Authorization: Bearer {{token}}
```

### 7. Get Articles
```
GET /api/v1/lookups/articles
Authorization: Bearer {{token}}
```

### 8. Get Document Costs
```
GET /api/v1/lookups/document-costs/123
Authorization: Bearer {{token}}
```

### 9. Get Cost Types
```
GET /api/v1/lookups/cost-types
Authorization: Bearer {{token}}
```

### 10. Get Cost Distribution Methods
```
GET /api/v1/lookups/cost-distribution-methods
Authorization: Bearer {{token}}

Response:
[
  { "id": 1, "naziv": "Po količini", "opis": "..." },
  { "id": 2, "naziv": "Po vrednosti", "opis": "..." },
  { "id": 3, "naziv": "Ručno", "opis": "..." }
]
```

### 11. Get Cost Articles
```
GET /api/v1/lookups/cost-articles/123
Authorization: Bearer {{token}}
```

---

## 📝 LINE ITEMS ENDPOINTS (sa ETag konkurentnosti)

### GET Items - Lista
```
GET /api/v1/documents/123/items
Authorization: Bearer {{token}}

Response:
[
  {
    "id": 1,
    "documentId": 123,
    "articleId": 456,
    "quantity": 5,
    "invoicePrice": 100.00,
    "total": 500.00,
    "eTag": "AQIDBAUGBwg="
  }
]
```

### GET Item - Jedan sa ETag Header-om
```
GET /api/v1/documents/123/items/1
Authorization: Bearer {{token}}

Response Header:
ETag: "AQIDBAUGBwg="

Response Body:
{
  "id": 1,
  "documentId": 123,
  "articleId": 456,
  "quantity": 5,
  "invoicePrice": 100.00,
  "total": 500.00,
  "eTag": "AQIDBAUGBwg="
}
```

### POST - Kreiraj Stavku
```
POST /api/v1/documents/123/items
Authorization: Bearer {{token}}
Content-Type: application/json

Request:
{
  "articleId": 456,
  "quantity": 5,
  "invoicePrice": 100.00,
  "discountAmount": 0,
  "marginAmount": 0,
  "taxRateId": "20",
  "calculateExcise": false,
  "calculateTax": true
}

Response: 201 Created
ETag Header: "AQIDBAUGBwg="
```

### 🔴 PATCH - Ažuriranja sa ETag Konkurentnosti (KRITIČNO!)

#### Scenario A: USPEŠNO
```
PATCH /api/v1/documents/123/items/1
Authorization: Bearer {{token}}
Content-Type: application/json
If-Match: "AQIDBAUGBwg="  ← OBAVEZNO!

Request:
{
  "quantity": 10,
  "invoicePrice": 150.00
}

Response: 200 OK
ETag Header: "AgMEBQYHCAk="  ← NOVI ETag!
```

#### Scenario B: 409 CONFLICT (stavka promenjena)
```
PATCH /api/v1/documents/123/items/1
Authorization: Bearer {{token}}
Content-Type: application/json
If-Match: "OLD_ETAG_VALUE"  ← Zastareo ETag

Response: 409 Conflict
{
  "message": "Stavka je promenjena od drugog korisnika",
  "detail": "Osvežite stranicu ili izaberite overwrite",
  "currentETag": "NEW_ETAG_VALUE",
  "timestamp": "2025-11-17T02:45:00Z"
}
```

#### Scenario C: 400 BAD REQUEST (nedostaje If-Match)
```
PATCH /api/v1/documents/123/items/1
Authorization: Bearer {{token}}
Content-Type: application/json

Response: 400 Bad Request
{
  "message": "Missing If-Match header (ETag required)"
}
```

### DELETE - Obriši Stavku
```
DELETE /api/v1/documents/123/items/1
Authorization: Bearer {{token}}

Response: 204 No Content
```

---

## 📊 TESTNI SCENARIO - 2 KORISNIKA

### Korisnik 1 - Pravi prvi PATCH
```
1. GET /api/v1/documents/123/items/1
   <- Response ETag: "AAA"
   
2. PATCH /api/v1/documents/123/items/1
   If-Match: "AAA"
   Quantity: 10
   <- Response ETag: "BBB" ✅
```

### Korisnik 2 - Pokušava da ažurira sa starim ETag-om
```
1. GET /api/v1/documents/123/items/1
   <- Response ETag: "AAA" (još uvek vidi staro)
   
2. PATCH /api/v1/documents/123/items/1
   If-Match: "AAA"
   Quantity: 20
   <- Response 409 Conflict ❌
   <- currentETag: "BBB"
   
3. GET /api/v1/documents/123/items/1  (osvežava)
   <- Response ETag: "BBB"
   
4. PATCH /api/v1/documents/123/items/1
   If-Match: "BBB"
   Quantity: 20
   <- Response ETag: "CCC" ✅
```

---

## 🧪 cURL Primeri

### Dobijanje stavke sa ETag-om
```bash
curl -H "Authorization: Bearer TOKEN" \
     http://localhost:5000/api/v1/documents/123/items/1
```

### PATCH sa If-Match
```bash
curl -X PATCH \
     -H "Authorization: Bearer TOKEN" \
     -H "If-Match: \"AQIDBAUGBwg=\"" \
     -H "Content-Type: application/json" \
     -d '{"quantity": 10}' \
     http://localhost:5000/api/v1/documents/123/items/1
```

### Proba 409 Conflict
```bash
curl -X PATCH \
     -H "Authorization: Bearer TOKEN" \
     -H "If-Match: \"WRONG_ETAG\"" \
     -H "Content-Type: application/json" \
     -d '{"quantity": 20}' \
     http://localhost:5000/api/v1/documents/123/items/1

# Response: 409 Conflict
```

---

## 📋 Postman Environment Varijable

```json
{
  "name": "ERP Accounting Development",
  "values": [
    {
      "key": "baseUrl",
      "value": "http://localhost:5000",
      "type": "string",
      "enabled": true
    },
    {
      "key": "token",
      "value": "YOUR_JWT_TOKEN_HERE",
      "type": "string",
      "enabled": true
    },
    {
      "key": "documentId",
      "value": "123",
      "type": "string",
      "enabled": true
    },
    {
      "key": "itemId",
      "value": "1",
      "type": "string",
      "enabled": true
    },
    {
      "key": "eTag",
      "value": "",
      "type": "string",
      "enabled": true
    }
  ]
}
```

---

## 🧩 Pre-request Script (Postman)

```javascript
// Ekstraktuj ETag iz prethodnog response-a
if (pm.response && pm.response.headers) {
    var etag = pm.response.headers.get("ETag");
    if (etag) {
        pm.environment.set("eTag", etag);
        console.log("ETag postavljan: " + etag);
    }
}
```

---

**Kreirano:** 17.11.2025  
**Verzija:** 1.0  
**API Verzija:** v1
