using ERPAccounting.API.Middleware;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.DTOs.Costs;
using ERPAccounting.Application.DTOs.Documents;
using ERPAccounting.Application.Mapping;
using ERPAccounting.Application.Services;
using ERPAccounting.Application.Validators;
using ERPAccounting.Common.Extensions;
using ERPAccounting.Infrastructure.Extensions;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentCostService, DocumentCostService>();
builder.Services.AddScoped<IDocumentLineItemService, DocumentLineItemService>();
builder.Services.AddAutoMapper(typeof(DocumentMappingProfile).Assembly);
builder.Services.AddScoped<IValidator<CreateDocumentDto>, CreateDocumentValidator>();
builder.Services.AddScoped<IValidator<UpdateDocumentDto>, UpdateDocumentValidator>();
builder.Services.AddScoped<IValidator<DocumentQueryParameters>, DocumentQueryParametersValidator>();
builder.Services.AddScoped<IValidator<CreateLineItemDto>, CreateLineItemValidator>();
builder.Services.AddScoped<IValidator<PatchLineItemDto>, PatchLineItemValidator>();
builder.Services.AddScoped<IValidator<CreateDocumentCostDto>, CreateDocumentCostValidator>();
builder.Services.AddScoped<IValidator<UpdateDocumentCostDto>, UpdateDocumentCostValidator>();
builder.Services.AddScoped<IValidator<CreateDocumentCostItemDto>, CreateDocumentCostItemValidator>();
builder.Services.AddScoped<IValidator<PatchDocumentCostItemDto>, PatchDocumentCostItemValidator>();
builder.Services.AddInfrastructureServices(builder.Configuration);

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
