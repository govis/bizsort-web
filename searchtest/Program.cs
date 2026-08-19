using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Model.Offering;
using BizSrt.Data.Extensions;

class Program
{
    static async Task Main(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True", x => x.UseNetTopologySuite())
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);

        var queryInput = new SearchInput
        {
            Category = 81,
            Location = 1,
            OfferingType = 0,
            InclFacets = new BizSrt.Model.Semantic.FacetFilter { NoFilters = 0 },
            ExclFacets = new BizSrt.Model.Semantic.FacetFilter { NoFilters = 0 },
            StartIndex = 0,
            Length = 500
        };

        using var dbContext = new AppDbContext(optionsBuilder.Options);

        Console.WriteLine("\n[OFFERING SEARCH DEBUG]\n");

        var activeOfferings = dbContext.Offerings.Where(p => p.Status == (byte)BizSrt.Model.Offering.Status.Active);
        IQueryable<BizSrt.Data.Entities.Offering> query = activeOfferings;

        var targetCat = (short)queryInput.Category;
        var categoryIdsQuery = dbContext.Categories_Unwound
            .Where(cu => cu.Parent == targetCat)
            .Select(cu => cu.Child);

        query = from p in query
                join cp in dbContext.CompanyOfferings on p.Id equals cp.Offering
                where cp.UnlistedType == (byte)BizSrt.Model.Offering.UnlistedType.Listed &&
                      (cp.Category == targetCat || categoryIdsQuery.Contains(cp.Category)) &&
                      (queryInput.OfferingType == 0 || (p.Type & queryInput.OfferingType) > 0)
                select p;

        query = query.ApplyFacets(dbContext, queryInput.InclFacets, queryInput.ExclFacets);

        IQueryable<long>? locationOfferingIds = null;
        var childLocations = dbContext.Locations_Unwound
            .Where(lu => lu.Parent == queryInput.Location)
            .Select(lu => lu.Child);

        locationOfferingIds = dbContext.CompanyOfferings
            .Where(cp => dbContext.CompanyOffices
                .Where(co => co.Location == queryInput.Location || childLocations.Contains(co.Location))
                .Any(co => co.Company == cp.Company))
            .Select(cp => cp.Offering)
            .Distinct();

        var sw = Stopwatch.StartNew();

        Console.WriteLine("\n--- EXECUTING CATEGORY MATCHES ---");
        var categoryMatches = await query.Select(p => new { p.Id, p.Created }).ToArrayAsync();
        Console.WriteLine($"Found {categoryMatches.Length} category matches in {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        Console.WriteLine("\n--- EXECUTING LOCATION MATCHES ---");
        var locationMatches = await locationOfferingIds.ToArrayAsync();
        Console.WriteLine($"Found {locationMatches.Length} location matches in {sw.ElapsedMilliseconds}ms");

        var locationSet = new System.Collections.Generic.HashSet<long>(locationMatches);
        var allMatchingIds = categoryMatches
            .Where(p => locationSet.Contains(p.Id))
            .OrderByDescending(p => p.Created)
            .Select(p => p.Id)
            .ToArray();

        Console.WriteLine($"\nFinal Intersect Count: {allMatchingIds.Length}");

        var total = allMatchingIds.Length;
        var pageIds = allMatchingIds
            .Skip(queryInput.StartIndex)
            .Take(queryInput.Length > 0 ? queryInput.Length : 20)
            .ToArray();

        Console.WriteLine($"Returned Page Count: {pageIds.Length}");

        if (total == 0) 
        {
            Console.WriteLine("No records found! Checking raw table data...");
            
            var activeOfferingCount = await dbContext.Offerings.CountAsync(o => o.Status == 1);
            Console.WriteLine($"Offerings with Status == 1 (Active): {activeOfferingCount}");

            var draftOfferingCount = await dbContext.Offerings.CountAsync(o => o.Status == 0);
            Console.WriteLine($"Offerings with Status == 0 (Draft): {draftOfferingCount}");

            var unlistedCount = await dbContext.CompanyOfferings.CountAsync(co => co.UnlistedType == 1);
            Console.WriteLine($"CompanyOfferings with UnlistedType == 1 (Unlisted): {unlistedCount}");

            var listedCount = await dbContext.CompanyOfferings.CountAsync(co => co.UnlistedType == 0);
            Console.WriteLine($"CompanyOfferings with UnlistedType == 0 (Listed): {listedCount}");

            var validMatchesCount = await dbContext.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) AS Value
                FROM [Offerings] AS [o]
                INNER JOIN [CompanyOfferings] AS [c] ON [o].[Id] = [c].[Offering]
                WHERE [c].[Category] = 81 OR EXISTS (
                    SELECT 1
                    FROM [Categories_Unwound] AS [c0]
                    WHERE [c0].[Parent] = 81 AND [c0].[Child] = [c].[Category])").ToArrayAsync();
            Console.WriteLine($"Total Matches (raw SQL): {validMatchesCount.FirstOrDefault()}");

            var nullStatus = await dbContext.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) AS Value
                FROM [Offerings] AS [o]
                WHERE [o].[Status] IS NULL").ToArrayAsync();
            Console.WriteLine($"Offerings with NULL Status: {nullStatus.FirstOrDefault()}");

            var nullUnlisted = await dbContext.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) AS Value
                FROM [CompanyOfferings] AS [c]
                WHERE [c].[UnlistedType] IS NULL").ToArrayAsync();
            Console.WriteLine($"CompanyOfferings with NULL UnlistedType: {nullUnlisted.FirstOrDefault()}");

            var activeUnlisted = await dbContext.Database.SqlQueryRaw<int>(@"
                SELECT COUNT(*) AS Value
                FROM [Offerings] AS [o]
                INNER JOIN [CompanyOfferings] AS [c] ON [o].[Id] = [c].[Offering]
                WHERE ([c].[Category] = 81 OR EXISTS (
                    SELECT 1
                    FROM [Categories_Unwound] AS [c0]
                    WHERE [c0].[Parent] = 81 AND [c0].[Child] = [c].[Category]))
                AND [o].[Status] = 1 AND [c].[UnlistedType] = 0").ToArrayAsync();
            Console.WriteLine($"Matches with Status=1 and UnlistedType=0: {activeUnlisted.FirstOrDefault()}");

        }
    }
}
