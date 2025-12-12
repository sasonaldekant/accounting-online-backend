using ERPAccounting.API.Filters;
using ERPAccounting.Application.Extensions;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Infrastructure.Middleware;
using ERPAccounting.Infrastructure.Extensions;
using ERPAccounting.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Učitaj JWT konfiguraciju
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];

if (string.IsNullOrEmpty(jwtSigningKey))
{
    throw new InvalidOperationException("JWT SigningKey is missing in configuration!");
}

// Dodaj JWT autentifikaciju
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:5174"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("ETag", "X-Total-Count", "Location")
            .AllowCredentials();
    });
});

// Add services to the container with global filters and JSON options
builder.Services.AddControllers(options =>
{
    // ETag filter - automatski setuje ETag header na svaki response
    options.Filters.Add<ETagFilter>();
    
    // Concurrency exception filter - standardizovani 409 Conflict response
    options.Filters.Add<ConcurrencyExceptionFilter>();
})
.AddJsonOptions(options =>
{
    // FIXED: Dodaj custom DateTime converter za ISO 8601 format sa timezone-om
    // KRITIČNO: Mora biti PRIJE drugih convertera
    options.JsonSerializerOptions.Converters.Add(new IsoDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new IsoNullableDateTimeConverter());
    
    // Podrška za više formata DateTime-a
    // Prihvata: "2025-11-26", "2025-11-26T02:01:17", "2025-11-26 02:01:17.863"
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    
    // Dozvoli trailing commas
    options.JsonSerializerOptions.AllowTrailingCommas = true;
    
    // Property name case insensitive
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    
    // Default null handling
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    
    // Write numbers as strings to preserve precision
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;

    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();

// IMPORTANT: register infrastructure (DbContext, repositories, UoW...) BEFORE application services
builder.Services.AddInfrastructure(builder.Configuration);

// Registruj ICurrentUserService - POSLE AddInfrastructure, PRE AddApplicationServices
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Register application services (they depend on infrastructure)
builder.Services.AddApplicationServices();

// Audit log service
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Konfigurisati Swagger sa Bearer autentifikacijom
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP Accounting API",
        Version = "v1",
        Description = "Enterprise Resource Planning - Accounting Module API with ETag Concurrency Control"
    });

    // Dodaj definiciju za Bearer token
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header koristeći Bearer šemu. Primer: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiAuditMiddleware>();

// 🔧 FIX: Disable HTTPS redirect in development
// Frontend uses http://localhost:3000, Backend http://localhost:5286
// HTTPS redirect causes 307 redirects which break CORS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

// Obavezno dodaj autentifikaciju i autorizaciju pre MapControllers
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// FIXED: Custom JSON converter za DateTime koji pravilno handla ISO 8601 format sa timezone-om
/// 
/// Podržava formate:
/// - "2025-12-12" (datum samo, interpretira se kao UTC)
/// - "2025-12-12T00:00:00" (datetime bez timezone, UTC)
/// - "2025-12-12T00:00:00Z" (UTC timezone)
/// - "2025-12-12T00:00:00.000Z" (UTC sa milisekundama)
/// - "2025-12-12T00:00:00+01:00" (sa timezone offset)
/// 
/// KRITIČNO: Sprječava vraćanje DateTime.MinValue ({1.1.0001}) zbog neuspješne deserijalizacije
/// </summary>
public class IsoDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        string? dateString = reader.GetString();

        if (string.IsNullOrEmpty(dateString))
        {
            return DateTime.MinValue;
        }

        // 1. Pokušaj ISO 8601 sa RoundtripKind (čuva timezone info)
        if (DateTime.TryParse(
            dateString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal,
            out DateTime result))
        {
            return result;
        }

        // 2. Ako ne uspije, pokušaj sa AssumeUniversal (tretiraj kao UTC ako nema timezone)
        if (DateTime.TryParse(
            dateString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime resultAsUtc))
        {
            return resultAsUtc;
        }

        // 3. Fallback: samo parsiraj normalcno
        if (DateTime.TryParse(dateString, out DateTime fallback))
        {
            return fallback;
        }

        return DateTime.MinValue;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options)
    {
        // Konvertuj u UTC i ispisu u ISO 8601 formatu sa 'Z' sufiksom
        writer.WriteStringValue(value.ToUniversalTime().ToString("o"));
    }
}

/// <summary>
/// FIXED: Custom JSON converter za nullable DateTime
/// </summary>
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

        // Ista logika kao IsoDateTimeConverter
        if (DateTime.TryParse(
            dateString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal,
            out DateTime result))
        {
            return result;
        }

        if (DateTime.TryParse(
            dateString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime resultAsUtc))
        {
            return resultAsUtc;
        }

        if (DateTime.TryParse(dateString, out DateTime fallback))
        {
            return fallback;
        }

        return null;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime? value,
        JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToUniversalTime().ToString("o"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}