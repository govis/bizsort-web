using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BizSrt.Worker.Foundation
{
    /// <summary>
    /// Modernized .NET 10 BackgroundService replacing both legacy Foundation.Worker and Engine.Worker<T>.
    /// It utilizes System.Threading.Channels to decouple database polling (Recall) from item processing (Work),
    /// executing them safely and concurrently without raw thread blocking.
    /// </summary>
    public abstract class AsyncQueueWorker<T> : BackgroundService
    {
        protected readonly ILogger _logger;
        private readonly Channel<T> _workQueue;

        protected TimeSpan TIME_INTERVAL = TimeSpan.FromSeconds(5);
        protected int WORK_QUEUE_THRESHOLD = 100;
        protected int DEFAULT_RECALL_RECORDS = 10;
        protected int MAX_RECALL_RECORDS = 40;

        protected AsyncQueueWorker(ILogger logger)
        {
            _logger = logger;
            
            // Channels natively handle backpressure, replacing the legacy pendingCount and ResetEvent logic.
            var options = new BoundedChannelOptions(WORK_QUEUE_THRESHOLD)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _workQueue = Channel.CreateBounded<T>(options);
        }

        /// <summary>
        /// Equivalent to legacy Recall1(int maxRecords). Queries the DB for pending work.
        /// </summary>
        protected abstract Task<T[]> RecallAsync(int maxRecords, CancellationToken cancellationToken);

        /// <summary>
        /// Equivalent to legacy Process(T workItem). Processes a single work item.
        /// </summary>
        protected abstract Task ProcessAsync(T workItem, CancellationToken cancellationToken);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{GetType().Name} started.");

            // Spawn concurrent Producer (polling DB) and Consumer (processing items) tasks
            var producer = Task.Run(() => ProducerLoopAsync(stoppingToken), stoppingToken);
            var consumer = Task.Run(() => ConsumerLoopAsync(stoppingToken), stoppingToken);

            await Task.WhenAll(producer, consumer);

            _logger.LogInformation($"{GetType().Name} stopped.");
        }

        private async Task ProducerLoopAsync(CancellationToken stoppingToken)
        {
            int currentRecallRecords = DEFAULT_RECALL_RECORDS;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Only fetch if the queue has capacity (backpressure)
                    if (_workQueue.Reader.Count < WORK_QUEUE_THRESHOLD / 2)
                    {
                        var backlog = await RecallAsync(currentRecallRecords, stoppingToken);

                        if (backlog != null && backlog.Length > 0)
                        {
                            foreach (var item in backlog)
                            {
                                await _workQueue.Writer.WriteAsync(item, stoppingToken);
                            }

                            // Legacy Exponential Backoff Logic
                            if (backlog.Length < DEFAULT_RECALL_RECORDS)
                                currentRecallRecords = DEFAULT_RECALL_RECORDS;
                            else if (currentRecallRecords <= MAX_RECALL_RECORDS)
                                currentRecallRecords *= 2;

                            // If we fetched items, loop immediately to fetch more if needed
                            continue; 
                        }
                        else
                        {
                            currentRecallRecords = DEFAULT_RECALL_RECORDS;
                        }
                    }

                    // Idle wait (replaces ManualResetEvent.WaitOne with a timeout)
                    await Task.Delay(TIME_INTERVAL, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break; // Graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in {GetType().Name} Producer (RecallCycle).");
                    await Task.Delay(TIME_INTERVAL, stoppingToken); // Backoff on error
                }
            }
            
            // Signal the consumer that no more items will be written
            _workQueue.Writer.Complete();
        }

        private async Task ConsumerLoopAsync(CancellationToken stoppingToken)
        {
            try
            {
                // ReadAllAsync natively blocks until items are available and handles graceful shutdown
                await foreach (var item in _workQueue.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await ProcessAsync(item, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing item in {GetType().Name} Consumer (WorkCycle).");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
        }
    }
}
