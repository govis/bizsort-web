$program = @"
using BizSrt.Worker.Jobs;
using BizSrt.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BizSort")));

builder.Services.AddHostedService<CompanyIndexerWorker>();

var host = builder.Build();
host.Run();
"@

Set-Content "C:\Bizsort\bizsort-web\background\Program.cs" $program

$appsettings = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ConnectionStrings": {
    "BizSort": "Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
  }
}
"@

Set-Content "C:\Bizsort\bizsort-web\background\appsettings.json" $appsettings

# Build the worker
cd C:\Bizsort\bizsort-web\background
dotnet build
