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

namespace BizSrt.Worker.Company.Offering
{
    public class OfferingIndexerWorkItem
    {
        public long OfferingId { get; set; }
    }

    /// <summary>
    /// Background worker that polls for stale or un-indexed offerings and triggers the heavy IndexOffering logic.
    /// Ported from legacy Engine.Offering.Index
    /// </summary>
    public class Indexer : AsyncQueueWorker<OfferingIndexerWorkItem>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly CompanyService.CompanyServiceClient _grpcClient;
        
        // Tracks items currently in the queue or being processed so we don't fetch them again
        private readonly ConcurrentDictionary<long, byte> _pendingItems = new();

        public Indexer(
            ILogger<Indexer> logger,
            CompanyService.CompanyServiceClient grpcClient,
            IServiceScopeFactory scopeFactory) : base(logger)
        {
            _scopeFactory = scopeFactory;
            _grpcClient = grpcClient;
        }

        protected override async Task<OfferingIndexerWorkItem[]> RecallAsync(int maxRecords, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var indexed10 = DateTime.UtcNow.AddDays(-10);
            var indexed30 = DateTime.UtcNow.AddDays(-30);
            var updated = DateTime.UtcNow.AddMinutes(-1);

            // Ported exactly from legacy \legacy\server\Engine\Offering\Index.cs:
            // var q = from cp in dc.CompanyOfferings
            //         join p in dc.Offerings on cp.Offering equals p.Id
            //         where (cp.Indexed != null && cp.Indexed.HasValue && ((/*(p.Type == (byte)Model.Offering.Type.Listed || p.Type == (byte)Model.Offering.Type.Unlisted) &&*/p.Status == (byte)Model.Offering.Status.Active && cp.Indexed.Value < indexed10) || cp.Indexed.Value < indexed30)) ||
            //         (cp.Indexed == null && (p.PendingStatus & (byte)Model.PendingStatus.EmailConfirmation) == 0 && p.Updated < updated)
            //         select p;

            var query = from cp in dbContext.CompanyOfferings
                        join p in dbContext.Offerings on cp.Offering equals p.Id
                        where (cp.Indexed != null && ((p.Status == (byte)BizSrt.Model.Offering.Status.Active && cp.Indexed < indexed10) || cp.Indexed < indexed30)) ||
                              (cp.Indexed == null && (p.PendingStatus & (byte)PendingStatus.EmailConfirmation) == 0 && p.Updated < updated)
                        select p.Id;

            // Fetch candidate IDs. We fetch a bit extra in case many are already pending
            var candidateIds = await query.Take(maxRecords * 2).ToArrayAsync(cancellationToken);

            var pendingKeys = _pendingItems.Keys.ToArray();
            
            // Filter out items already in the queue or processing
            var newIds = candidateIds.Where(id => !pendingKeys.Contains(id)).Take(maxRecords).ToArray();

            var items = new OfferingIndexerWorkItem[newIds.Length];
            for (int i = 0; i < newIds.Length; i++)
            {
                var id = newIds[i];
                _pendingItems.TryAdd(id, 1);
                items[i] = new OfferingIndexerWorkItem { OfferingId = id };
            }

            return items;
        }

        protected override async Task ProcessAsync(OfferingIndexerWorkItem workItem, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Sending gRPC request to index Offering {workItem.OfferingId}...");
                var request = new IndexOfferingRequest { OfferingId = workItem.OfferingId };
                await _grpcClient.IndexOfferingAsync(request, cancellationToken: cancellationToken); 
                _logger.LogInformation($"Successfully indexed Offering {workItem.OfferingId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to index Offering {workItem.OfferingId} via gRPC.");
                throw;
            }
            finally
            {
                _pendingItems.TryRemove(workItem.OfferingId, out _);
            }
        }
    }
}
