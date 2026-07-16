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
        optionsBuilder.UseSqlServer("Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True", x => x.UseNetTopologySuite());
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

        Console.WriteLine("Running LINQNew...");
        try {
            var newResult = await SearchParityTest.CompanySearchLINQNew(dbContext, input);
            Console.WriteLine($"LINQNew: TotalCount = {newResult.TotalCount}, Series Length = {newResult.Series?.Length ?? 0}");
        } catch (Exception ex) {
            Console.WriteLine($"LINQNew Error: {ex.Message}");
        }

        Console.WriteLine("Running LINQUnion...");
        try {
            var unionResult = await SearchParityTest.CompanySearchLINQUnion(dbContext, input);
            Console.WriteLine($"LINQUnion: TotalCount = {unionResult.TotalCount}, Series Length = {unionResult.Series?.Length ?? 0}");
        } catch (Exception ex) {
            Console.WriteLine($"LINQUnion Error: {ex.Message}");
        }

        Console.WriteLine("Running LINQOld...");
        try {
            var oldResult = await SearchParityTest.CompanySearchLINQOld(dbContext, input);
            Console.WriteLine($"LINQOld: TotalCount = {oldResult.TotalCount}, Series Length = {oldResult.Series?.Length ?? 0}");
        } catch (Exception ex) {
            Console.WriteLine($"LINQOld Error: {ex.Message}");
        }

        Console.WriteLine("Running SQL...");
        try {
            var sqlResult = await SearchParityTest.CompanySearchSQL(dbContext, input);
            Console.WriteLine($"SQL: TotalCount = {sqlResult.TotalCount}, Series Length = {sqlResult.Series?.Length ?? 0}");
        } catch (Exception ex) {
            Console.WriteLine($"SQL Error: {ex.Message}");
        }
    }
}
