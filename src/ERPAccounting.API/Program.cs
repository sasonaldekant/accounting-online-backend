using ERPAccounting.Application.Extensions;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Infrastructure.Middleware;
using ERPAccounting.Infrastructure.Extensions;
using ERPAccounting.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

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

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// IMPORTANT: register infrastructure (DbContext, repositories, UoW...) BEFORE application services
builder.Services.AddInfrastructure(builder.Configuration);

// Registruj ICurrentUserService - POSLE AddInfrastructure, PRE AddApplicationServices
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Register application services (they depend on infrastructure)
builder.Services.AddApplicationServices();

// Audit log service
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Konfiguriši Swagger sa Bearer autentifikacijom
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP Accounting API",
        Version = "v1",
        Description = "Enterprise Resource Planning - Accounting Module API"
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

app.UseHttpsRedirection();

// Obavezno dodaj autentifikaciju i autorizaciju pre MapControllers
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();