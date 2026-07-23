using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Model.Company;
using BizSrt.Model.List;
using BizSrt.SearchTest;

class Program
{
    static async Task Main(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True", x => x.UseNetTopologySuite())
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Warning);

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

        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        Company Search Performance Benchmark Suite           ║");
        Console.WriteLine("║        Category=163  Location=1  TransactionType=3          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ── Warm the SQL connection ──
        using (var warmCtx = new AppDbContext(optionsBuilder.Options))
        {
            Console.WriteLine("[WARMUP] Establishing connection and warming EF Core...");
            await warmCtx.Database.ExecuteSqlRawAsync("SELECT 1");
            Console.WriteLine("[WARMUP] Done.\n");
        }

        // ── Test 1: Current LINQNew (the winner from previous round) ──
        await RunBenchmark("LINQNew (Current Best)", optionsBuilder, input, 3,
            (db, inp) => SearchParityTest.CompanySearchLINQNew(db, inp));

        // ── Test 2: LINQNew Variant A — Combined SQL (single query, no split) ──
        await RunBenchmark("LINQNew Variant A: Combined SQL (No Split)", optionsBuilder, input, 3,
            (db, inp) => SearchParityTest.CompanySearchLINQCombined(db, inp));

        // ── Test 3: LINQNew Variant B — OPENJSON for Location ──
        await RunBenchmark("LINQNew Variant B: OPENJSON Location", optionsBuilder, input, 3,
            (db, inp) => SearchParityTest.CompanySearchLINQOpenJsonLocation(db, inp));

        // ── Test 4: LINQNew Variant C — EXISTS-based Location (no Distinct) ──
        await RunBenchmark("LINQNew Variant C: EXISTS Location", optionsBuilder, input, 3,
            (db, inp) => SearchParityTest.CompanySearchLINQExistsLocation(db, inp));

        // ── Test 5: LINQNew Variant D — SQL-side ORDER BY (backward scan re-test) ──
        await RunBenchmark("Variant D: SQL-side ORDER BY (backward scan)", optionsBuilder, input, 3,
            (db, inp) => SearchParityTest.CompanySearchLINQSqlOrderBy(db, inp));

        // ── Test 6: LINQNew Variant E — GroupBy+First Office Map ──
        await RunBenchmark("Variant E: GroupBy+First Office Map", optionsBuilder, input, 3,
            (db, inp) => SearchParityTest.CompanySearchLINQGroupByOffice(db, inp));

        // ── Test 7: LINQNew Variant F — ROW_NUMBER() Office Map ──
        await RunBenchmark("Variant F: ROW_NUMBER() Office Map", optionsBuilder, input, 3,
            (db, inp) => SearchParityTest.CompanySearchLINQRowNumberOffice(db, inp));

        // ── Test 8: LINQOld (TVF + triple query penalty) ──
        await RunBenchmark("LINQOld (TVF + Triple Query)", optionsBuilder, input, 3,
            (db, inp) => SearchParityTest.CompanySearchLINQOld(db, inp));

        // ── Test 9: SP Baseline ──
        await RunBenchmark("LINQSQL (SP Baseline)", optionsBuilder, input, 3,
            (db, inp) => SearchParityTest.CompanySearchSQL(db, inp));

        Console.WriteLine("\n[DONE] All benchmarks complete.");
    }

    static async Task RunBenchmark(
        string name,
        DbContextOptionsBuilder<AppDbContext> optionsBuilder,
        SearchInput input,
        int iterations,
        Func<AppDbContext, SearchInput, Task<SearchOutput<SearchItem>>> testFunc)
    {
        Console.WriteLine($"┌─────────────────────────────────────────────────────────────┐");
        Console.WriteLine($"│ {name,-58}│");
        Console.WriteLine($"└─────────────────────────────────────────────────────────────┘");

        for (int i = 0; i < iterations; i++)
        {
            var label = i == 0 ? "cold" : $"warm-{i}";
            using var dbContext = new AppDbContext(optionsBuilder.Options);

            // Warm connection for this context
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");

            // Flush plan cache before cold run
            if (i == 0)
            {
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync("DBCC FREEPROCCACHE");
                    Console.WriteLine($"  [plan cache flushed]");
                }
                catch { /* ignore if no permission */ }
            }

            try
            {
                var sw = Stopwatch.StartNew();
                var result = await testFunc(dbContext, input);
                sw.Stop();
                Console.WriteLine($"  Run {i + 1} ({label}): TotalCount={result.TotalCount}, Page={result.Series?.Length ?? 0}, Facets={result.Facets?.Length ?? 0}, Time={sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Run {i + 1} ({label}): ERROR — {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"    Inner: {ex.InnerException.Message}");
            }
        }
        Console.WriteLine();
    }
}
