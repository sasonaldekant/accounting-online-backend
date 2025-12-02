using ERPAccounting.API.Filters;
using ERPAccounting.Application.Extensions;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Infrastructure.Middleware;
using ERPAccounting.Infrastructure.Extensions;
using ERPAccounting.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
