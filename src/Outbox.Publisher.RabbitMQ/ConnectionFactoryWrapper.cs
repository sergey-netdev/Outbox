using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Outbox.Publisher.RabbitMQ;

/// <summary>
/// A wrapper
/// </summary>
public class ConnectionFactoryWrapper
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger _logger;
    private bool _dispose = false;
    private IConnection _connection;
    private ManualResetEventSlim _e = new();

    public ConnectionFactoryWrapper(IConnectionFactory connectionFactory, ILogger<ConnectionFactoryWrapper> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IConnection> CreateConnectionAsync(TimeSpan timeout)
    {
        Task<IConnection> backgroundTask = Task.Factory.StartNew(CreateConnection);

        try
        {
            IConnection result = await backgroundTask.WaitAsync(timeout).ConfigureAwait(false);
            ////// capture possibly created connection while the task waits on the event
            ////result = _connection;
            _dispose = false;
            return result;
        }
        catch (System.TimeoutException ex)
        {
            _dispose = true;
            throw new TimeoutException(string.Format(TimeoutException.IntervalExceededMessage, timeout), ex);
        }

        throw new InvalidOperationException();
    }

    protected IConnection CreateConnection()
    {
        try
        {
            _connection = _connectionFactory.CreateConnection();
            //_e.Wait();
            
            // if the connection was created after the timeout was reached
            // we must deallocate it - nobody is interested in it
            if (_dispose)
            {
                try
                {
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot dispose connection."); // nothing we can do in the background thread
                }
            }

            return _connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot create connection.");
            throw;
        }
    }
}
