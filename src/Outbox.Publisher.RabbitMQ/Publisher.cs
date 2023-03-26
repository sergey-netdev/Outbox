namespace Outbox.Publisher.RabbitMQ;

using global::RabbitMQ.Client;
using Outbox.Core;
using System.Threading.Tasks;

public class Publisher : IPublisher, IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly Lazy<IConnection> _connection;
    private readonly PublisherOptions _options;
    private bool _disposed;

    public Publisher(IConnectionFactory connectionFactory, PublisherOptions options)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connection = new(() => _connectionFactory.CreateConnection(), true);
    }

    private IConnection Connection => _connection.Value;

    public Task PublishAsync(IOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        if (_disposed)
        {
            throw new ObjectDisposedException(null);
        }

        return this.PublishAsyncInternal(message);
    }

    private async Task PublishAsyncInternal(IOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        if (_disposed)
        {
            throw new ObjectDisposedException(null);
        }

        await Task.Delay(0);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_connection.IsValueCreated)
                {
                    _connection.Value.Dispose();
                }
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
