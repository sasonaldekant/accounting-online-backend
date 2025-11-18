using ERPAccounting.API.Middleware;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.DTOs.LineItems;
using ERPAccounting.Application.Services;
using ERPAccounting.Application.Services.Contracts;
using ERPAccounting.Application.Validators;
using ERPAccounting.Infrastructure.Data;
using ERPAccounting.Infrastructure.Repositories;
using ERPAccounting.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentCostService, DocumentCostService>();
builder.Services.AddScoped<IDocumentLineItemService, DocumentLineItemService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ERPAccounting"));
builder.Services.AddScoped<IDocumentLineItemRepository, DocumentLineItemRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IValidator<CreateLineItemDto>, CreateLineItemValidator>();
builder.Services.AddScoped<IValidator<PatchLineItemDto>, PatchLineItemValidator>();
builder.Services.AddScoped<IStoredProcedureGateway, StoredProcedureGateway>();
builder.Services.AddScoped<ILookupService, StoredProcedureService>();

var app = builder.Build();

app.UseDomainExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
