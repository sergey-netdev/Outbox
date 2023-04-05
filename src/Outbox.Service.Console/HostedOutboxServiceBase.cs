namespace Outbox.Service.Console;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;

public abstract class HostedOutboxServiceBase : BackgroundService
{
    private readonly ProcessBatch _processDelegate;
    private readonly ILogger _logger;
    private readonly int _batchSize;
    private readonly TimeSpan _sleepInterval;

    public delegate Task<int> ProcessBatch(int batchSize, CancellationToken cancellationToken);

    public HostedOutboxServiceBase(
        ProcessBatch processDelegate,
        int batchSize,
        TimeSpan sleepInterval,
        ILogger logger)
    {
        _processDelegate = processDelegate ?? throw new ArgumentNullException(nameof(processDelegate));
        _batchSize = batchSize > 0 ? batchSize : throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Must be positive number.");
        _sleepInterval = sleepInterval;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int messageCount = int.MaxValue;
            try
            {
                messageCount = await _processDelegate(_batchSize, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex!, string.Empty);
            }

            _logger.LogInformation("Processed {messageCount} message(s).", messageCount);

            if (messageCount < _batchSize)
            {
                // sleep, if we got less messages than requested, otherwise immediately process the next batch
                _logger.LogTrace("Sleeping...");
                await Task.Delay(_sleepInterval, stoppingToken);
            }
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting with {batchSize} batch size and {sleepInterval} interval...", _batchSize, _sleepInterval);
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping ({cancelled})...", cancellationToken.IsCancellationRequested);
        return base.StopAsync(cancellationToken);
    }
}
