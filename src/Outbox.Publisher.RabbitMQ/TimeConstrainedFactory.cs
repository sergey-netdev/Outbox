using Microsoft.Extensions.Logging;

namespace Outbox.Publisher.RabbitMQ;

/// <summary>
/// A wrapper around a simple synchronous factory method <see cref="Func{TResult}"/>
/// that is limited by a timeout.
/// </summary>
public class TimeConstrainedFactory<T>
{
    private readonly Func<T> _factoryMethod;
    private readonly ILogger _logger;

    public TimeConstrainedFactory(Func<T> factoryMethod, ILogger<TimeConstrainedFactory<T>> logger)
    {
        _factoryMethod = factoryMethod ?? throw new ArgumentNullException(nameof(factoryMethod));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> CreateAsync(TimeSpan timeout)
    {
        Task<T> backgroundTask = Task.Factory.StartNew(_factoryMethod);

        try
        {
            T result = await backgroundTask.WaitAsync(timeout).ConfigureAwait(false);
            return result;
        }
        catch (System.TimeoutException ex)
        {
            // add a continuation to cleanup if it runs to comppletion
            // and DO NOT await it
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            backgroundTask
                .ContinueWith(t => { Cleanup(t.Result); }, TaskContinuationOptions.OnlyOnRanToCompletion)
                .ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            throw new TimeoutException(string.Format(TimeoutException.IntervalExceededMessage, timeout), ex);
        }
    }

    protected virtual void Cleanup(T @object)
    {
        if (@object is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot dispose object."); // all we can do
            }
        }
    }
}
