using ERPAccounting.API.Extensions;
using ERPAccounting.API.Middleware;
using ERPAccounting.Application.Extensions;
using ERPAccounting.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Swagger setup with Bearer auth so "Authorize" button appears ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ERPAccounting.API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token as: **Bearer &lt;token&gt;**"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});
// -------------------------------------------------------------------

// IMPORTANT: register infrastructure (DbContext, repositories, UoW...) BEFORE application services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Ensure critical repositories are registered even if the infrastructure
// bootstrap changes. This explicitly wires the document line item repository
// required by DocumentLineItemService to prevent runtime DI failures.
builder.Services.AddScoped<ERPAccounting.Domain.Abstractions.Repositories.IDocumentLineItemRepository,
    ERPAccounting.Infrastructure.Repositories.DocumentLineItemRepository>();

// Register application services (they depend on infrastructure)
builder.Services.AddApplicationServices();

// JWT Authentication configuration
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var signingKey = jwtSection.GetValue<string>("SigningKey")
            ?? throw new InvalidOperationException("JWT SigningKey configuration is missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection.GetValue<string>("Issuer"),
            ValidAudience = jwtSection.GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
    });

var app = builder.Build();

// Custom domain-level exception handling if defined
app.UseDomainExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERPAccounting.API v1");
        // c.RoutePrefix = string.Empty; // optional: serve Swagger UI at app root
    });
}

// app middleware order
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
