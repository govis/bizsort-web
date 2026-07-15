using BizSrt.Worker.Company;
using BizSrt.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BizSort")));

builder.Services.AddGrpcClient<BizSrt.Model.Grpc.CompanyService.CompanyServiceClient>(o =>
{
    o.Address = new Uri("http://localhost:5000");
});
builder.Services.AddHostedService<Indexer>();

var host = builder.Build();
host.Run();

