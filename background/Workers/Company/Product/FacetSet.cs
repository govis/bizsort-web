using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BizSrt.Data;
using BizSrt.Worker.Foundation;

namespace BizSrt.Worker.Company.Product
{
    public class FacetSetWorkItem
    {
        public int SetId { get; set; }
        public ActionType Action { get; set; }

        public enum ActionType : byte
        {
            Index = 0,
            Delete = 1
        }
    }

    /// <summary>
    /// Background worker that polls for un-indexed or stale product facet sets.
    /// Ported from legacy Engine.Product.FacetSet
    /// </summary>
    public class FacetSet : AsyncQueueWorker<FacetSetWorkItem>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly BizSrt.Model.Grpc.CompanyService.CompanyServiceClient _grpcClient;

        public FacetSet(
            ILogger<FacetSet> logger,
            BizSrt.Model.Grpc.CompanyService.CompanyServiceClient grpcClient,
            IServiceScopeFactory scopeFactory) : base(logger)
        {
            _scopeFactory = scopeFactory;
            _grpcClient = grpcClient;
        }

        protected override async Task<FacetSetWorkItem[]> RecallAsync(int maxRecords, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pending = _workQueue.Select(pfs => pfs.SetId).ToArray();

            var items = new System.Collections.Generic.List<FacetSetWorkItem>();

            // Phase 1: Index
            var lastUsedIndex = DateTime.UtcNow.AddMinutes(-1);
            var queryIndex = from pfs in dbContext.CompanyProductFacetSets
                             where pfs.Indexed == null && pfs.LastUsed < lastUsedIndex
                             select pfs.Id;

            var idsToIndex = await queryIndex.Take(maxRecords).ToArrayAsync(cancellationToken);
            if (pending.Length > 0)
                idsToIndex = idsToIndex.Where(id => !pending.Contains(id)).ToArray();

            items.AddRange(idsToIndex.Select(id => new FacetSetWorkItem { SetId = id, Action = FacetSetWorkItem.ActionType.Index }));

            // Phase 2: Delete
            if (items.Count < maxRecords)
            {
                var maxDelete = maxRecords - items.Count;
                var lastUsedDelete = DateTime.UtcNow.AddDays(-1);

                var queryDelete = from pfs in dbContext.CompanyProductFacetSets
                                  where pfs.LastUsed < lastUsedDelete
                                  select pfs.Id;

                var idsToDelete = await queryDelete.Take(maxDelete).ToArrayAsync(cancellationToken);
                
                items.AddRange(idsToDelete.Select(id => new FacetSetWorkItem { SetId = id, Action = FacetSetWorkItem.ActionType.Delete }));
            }

            return items.ToArray();
        }

        protected override async Task ProcessAsync(FacetSetWorkItem workItem, CancellationToken cancellationToken)
        {
            try
            {
                switch (workItem.Action)
                {
                    case FacetSetWorkItem.ActionType.Index:
                        _logger.LogInformation($"Sending gRPC request to index Product FacetSet {workItem.SetId}...");
                        await _grpcClient.IndexProductFacetSetAsync(new BizSrt.Model.Grpc.IndexProductFacetSetRequest { SetId = workItem.SetId }, cancellationToken: cancellationToken);
                        _logger.LogInformation($"Successfully indexed Product FacetSet {workItem.SetId}.");
                        break;
                    case FacetSetWorkItem.ActionType.Delete:
                        _logger.LogInformation($"Sending gRPC request to delete Product FacetSet {workItem.SetId}...");
                        await _grpcClient.DeleteProductFacetSetAsync(new BizSrt.Model.Grpc.DeleteProductFacetSetRequest { SetId = workItem.SetId }, cancellationToken: cancellationToken);
                        _logger.LogInformation($"Successfully deleted Product FacetSet {workItem.SetId}.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process Product FacetSet {workItem.SetId} via gRPC.");
                throw;
            }
        }
    }
}
