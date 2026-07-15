using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Worker.Foundation;
using BizSrt.Model;
using BizSrt.Model.Grpc;

namespace BizSrt.Worker.Company
{
    public class CompanyIndexerWorkItem
    {
        public int CompanyId { get; set; }
    }

    /// <summary>
    /// Background worker that polls for stale or un-indexed companies and triggers the heavy IndexCompany logic.
    /// Ported from legacy Engine.Company.Index
    /// </summary>
    public class Indexer : AsyncQueueWorker<CompanyIndexerWorkItem>
    {
                private readonly IServiceScopeFactory _scopeFactory;
        private readonly CompanyService.CompanyServiceClient _grpcClient;
        
        // Tracks items currently in the queue or being processed so we don't fetch them again
        private readonly ConcurrentDictionary<int, byte> _pendingItems = new();

        public Indexer(
            ILogger<Indexer> logger,
            CompanyService.CompanyServiceClient grpcClient,
            IServiceScopeFactory scopeFactory) : base(logger)
        {
            _scopeFactory = scopeFactory;
            _grpcClient = grpcClient;
        }

        protected override async Task<CompanyIndexerWorkItem[]> RecallAsync(int maxRecords, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var indexed10 = DateTime.UtcNow.AddDays(-10);
            var indexed30 = DateTime.UtcNow.AddDays(-30);
            var updated = DateTime.UtcNow.AddMinutes(-1);

            // Ported exactly from legacy legacy\server\Engine\Company\Index.cs:
            // var q = from c in dc.CompanyProfiles
            //         join a in dc.Accounts on c.Id equals a.Id
            //         where (c.Indexed != null && c.Indexed.HasValue && ((a.Status == (byte)Model.Account.Status.Active && c.Indexed.Value < indexed10) || c.Indexed.Value < indexed30)) ||
            //         (c.Indexed == null && (c.PendingStatus & (byte)Model.Account.PendingStatus.EmailConfirmation) == 0 && c.Updated < updated)
            //         select c;

            var query = from c in dbContext.CompanyProfiles
                        join a in dbContext.Accounts on c.Id equals a.Id
                        where (c.Indexed != null && ((a.Status == (byte)Status.Active && c.Indexed < indexed10) || c.Indexed < indexed30)) ||
                              (c.Indexed == null && (c.PendingStatus & (byte)PendingStatus.EmailConfirmation) == 0 && c.Updated < updated)
                        select c.Id;

            // Fetch candidate IDs. We fetch a bit extra in case many are already pending
            var candidateIds = await query.Take(maxRecords * 2).ToArrayAsync(cancellationToken);

            var pendingKeys = _pendingItems.Keys.ToArray();
            
            // Filter out items already in the queue or processing
            var newIds = candidateIds.Where(id => !pendingKeys.Contains(id)).Take(maxRecords).ToArray();

            var items = new CompanyIndexerWorkItem[newIds.Length];
            for (int i = 0; i < newIds.Length; i++)
            {
                var id = newIds[i];
                _pendingItems.TryAdd(id, 1);
                items[i] = new CompanyIndexerWorkItem { CompanyId = id };
            }

            return items;
        }

                protected override async Task ProcessAsync(CompanyIndexerWorkItem workItem, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Sending gRPC request to index Company {workItem.CompanyId}...");
                var request = new IndexCompanyRequest { CompanyId = workItem.CompanyId };
                await _grpcClient.IndexCompanyAsync(request, cancellationToken: cancellationToken);
                _logger.LogInformation($"Successfully indexed Company {workItem.CompanyId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to index Company {workItem.CompanyId} via gRPC.");
                throw;
            }
            finally
            {
                _pendingItems.TryRemove(workItem.CompanyId, out _);
            }
        }
    }
}
