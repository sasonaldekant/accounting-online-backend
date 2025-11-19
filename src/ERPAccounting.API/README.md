# ERPAccounting.API

**Web API Layer** - ASP.NET Core 8.0

## Odgovornosti
- RESTful API endpoints
- HTTP request/response handling
- Dependency Injection setup
- Middleware configuration
- Global exception handling extensions
- CORS policy
- Swagger/OpenAPI documentation

## Struktura
```
Controllers/
├── DocumentsController.cs
├── DocumentItemsController.cs
├── DocumentCostsController.cs
└── LookupsController.cs

Extensions/
└── ExceptionHandlingExtensions.cs

Program.cs                  # Entry point & DI
appsettings.json            # Configuration
appsettings.Development.json
```

## Dependencies
- ERPAccounting.Application
- ERPAccounting.Common
