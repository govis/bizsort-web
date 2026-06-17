using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Data;
using BizSrt.Api.Services;
using BizSrt.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var connectionString = builder.Configuration.GetConnectionString("BizSort") 
    ?? throw new InvalidOperationException("Connection string 'BizSort' not found.");

// --- Services (DI) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core with Spatial Support
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, x => x.UseNetTopologySuite()));

// Domain Services
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddSingleton<IImageService, ImageService>();

var app = builder.Build();

// --- Middleware Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- Endpoints ---
app.MapCompanyEndpoints();
app.MapImageEndpoints();

app.Run();
