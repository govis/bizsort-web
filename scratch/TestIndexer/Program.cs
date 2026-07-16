using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
var connectionString = "Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
optionsBuilder.UseSqlServer(connectionString, x => x.UseNetTopologySuite());

using var dbContext = new AppDbContext(optionsBuilder.Options);

var facets = dbContext.CompanyFacets.Where(c => c.Company == 1).ToList();
Console.WriteLine($"Found {facets.Count} facets for Company 1");
foreach (var f in facets)
{
    Console.WriteLine($"  Id: {f.Id}, FacetValue: {f.FacetValue}, UserDefined: {f.UserDefined}");
}
