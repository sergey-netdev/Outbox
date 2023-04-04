namespace Outbox.Service;
using Microsoft.Extensions.Logging;

public abstract class OutboxServiceBase
{
    public delegate Task<int> ProcessBatch(int batchSize, CancellationToken cancellationToken);

    public static Task RunAsync(ILogger logger, int batchSize, TimeSpan sleepInterval, ProcessBatch processDelegate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(processDelegate, nameof(processDelegate));
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Must be positive number.");
        }

        return RunAsyncInternal(logger, batchSize, sleepInterval, processDelegate, cancellationToken);
    }

    private static async Task RunAsyncInternal(ILogger logger, int batchSize, TimeSpan sleepInterval, ProcessBatch processDelegate, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Entering the main loop.");

        while (!cancellationToken.IsCancellationRequested)
        {
            int messageCount = int.MaxValue;
            try
            {
                messageCount = await processDelegate(batchSize, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex!, string.Empty);
            }

            if (messageCount < batchSize)
            {
                // sleep, if we got less messages than requested, otherwise immediately process the next batch
                logger.LogTrace("Sleeping...");
                await Task.Delay(sleepInterval, cancellationToken);
            }
        }
    }
}
