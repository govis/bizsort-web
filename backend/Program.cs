using Microsoft.EntityFrameworkCore;
using BizSrt.Api.Data;
using BizSrt.Api.Service;
using BizSrt.Api.Service.Company;
using BizSrt.Api.Endpoint;
using BizSrt.Api.Service.Master;
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
builder.Services.AddScoped<BizSrt.Api.Data.Company.ICompanyService, BizSrt.Api.Data.Company.CompanyService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ILocationService, LocationService>();
// builder.Services.AddSingleton<BizSrt.Api.Data.Cache.Company.CompanyProfilesCache>();
builder.Services.AddSingleton<IImageService, ImageService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- Middleware Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseHttpsRedirection();

// --- Endpoints ---
app.MapCompanyEndpoints();
app.MapCategoryEndpoints();
app.MapLocationEndpoints();
app.MapImageEndpoints();

app.Run();
