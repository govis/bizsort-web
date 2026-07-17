using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Model.Company;
using BizSrt.SearchTest;

class Program
{
    static async Task Main(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True", x => x.UseNetTopologySuite())
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
        using var dbContext = new AppDbContext(optionsBuilder.Options);

        var input = new SearchInput
        {
            Category = 163,
            Location = 1,
            TransactionType = 3,
            InclFacets = new BizSrt.Model.Semantic.FacetFilter { NoFilters = 0 },
            ExclFacets = new BizSrt.Model.Semantic.FacetFilter { NoFilters = 0 },
            StartIndex = 0,
            Length = 0
        };

        // Warm the SQL connection and EF Core internals before timing.
        // This isolates the search query cost from ADO.NET connection establishment
        // and EF Core cold-start overhead (~3-4s on first call).
        Console.WriteLine("Warming connection...");
        await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");

        Console.WriteLine("Running LINQNew (cold plan)...");
        try {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var newResult = await SearchParityTest.CompanySearchLINQNew(dbContext, input);
            sw.Stop();
            Console.WriteLine($"LINQNew (cold): TotalCount = {newResult.TotalCount}, Series Length = {newResult.Series?.Length ?? 0}, Time = {sw.ElapsedMilliseconds}ms");
        } catch (Exception ex) {
            Console.WriteLine($"LINQNew Error: {ex.Message}");
        }

        Console.WriteLine("Running LINQNew (warm plan)...");
        try {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var newResult = await SearchParityTest.CompanySearchLINQNew(dbContext, input);
            sw.Stop();
            Console.WriteLine($"LINQNew (warm): TotalCount = {newResult.TotalCount}, Series Length = {newResult.Series?.Length ?? 0}, Time = {sw.ElapsedMilliseconds}ms");
        } catch (Exception ex) {
            Console.WriteLine($"LINQNew Error: {ex.Message}");
        }

        Console.WriteLine("Running LINQSQL...");
        try {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sqlResult = await SearchParityTest.CompanySearchSQL(dbContext, input);
            sw.Stop();
            Console.WriteLine($"LINQSQL: TotalCount = {sqlResult.TotalCount}, Series Length = {sqlResult.Series?.Length ?? 0}, Time = {sw.ElapsedMilliseconds}ms");
        } catch (Exception ex) {
            Console.WriteLine($"LINQSQL Error: {ex.Message}");
        }
    }
}
