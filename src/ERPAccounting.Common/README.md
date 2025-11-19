# ERPAccounting.Common

**Shared Utilities & Cross-Cutting Concerns**

## Odgovornosti
- Custom exceptions
- Constants (API routes, error poruke, kodovi)
- Common response modeli (ProblemDetails)
- Helpers

## Struktura
```
Exceptions/
├── DomainException.cs
├── ConflictException.cs
├── NotFoundException.cs
└── ValidationException.cs

Constants/
├── ApiRoutes.cs
├── ErrorCodes.cs
└── ErrorMessages.cs

Models/
├── ProblemDetailsDto.cs
└── ConflictDetailsDto.cs
```

## Key Classes

### DomainException + izvedene klase
```csharp
throw new NotFoundException(ErrorMessages.DocumentLineItemNotFound, itemId.ToString(), "DocumentLineItem");
```

### ProblemDetailsDto
Standardizovani odgovor za greške (kompatibilan sa RFC7807) koji se koristi i u API sloju i kroz globalni exception handler (`UseDomainExceptionHandling`).
