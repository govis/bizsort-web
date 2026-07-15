using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
var connectionString = "Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
optionsBuilder.UseSqlServer(connectionString, x => x.UseNetTopologySuite());

using var dbContext = new AppDbContext(optionsBuilder.Options);

var company = dbContext.CompanyProfiles.OrderBy(c => c.Id).FirstOrDefault();
if (company != null)
{
    company.Indexed = null;
    dbContext.SaveChanges();
    Console.WriteLine($"Set Indexed = null for Company {company.Id}");
}
else
{
    Console.WriteLine("No company found.");
}
