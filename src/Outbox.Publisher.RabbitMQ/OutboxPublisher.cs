namespace Outbox.Publisher.RabbitMQ;

using global::RabbitMQ.Client;
using global::RabbitMQ.Client.Events;
using global::RabbitMQ.Client.Exceptions;
using Outbox.Core;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;

public class OutboxPublisher : IOutboxPublisher, IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly Lazy<IConnection> _connection;
    private readonly OutboxPublisherOptions _options;
    private bool _disposed;

    public OutboxPublisher(IConnectionFactory connectionFactory, OutboxPublisherOptions options)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connection = new(() => _connectionFactory.CreateConnection(), true);
    }

    internal OutboxPublisherOptions Options => _options;

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

    private Task PublishAsyncInternal(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        DeliveryException? deliveryException = null;
        bool acked = false, nacked = false, confirmed = false;

        Stopwatch sw = new Stopwatch();
        Stopwatch sw1 = new Stopwatch();
        sw.Start();
        try
        {
            //cancellationToken.ThrowIfCancellationRequested();
            using IModel channel = this.Connection.CreateModel();
            //cancellationToken.ThrowIfCancellationRequested();
            sw1.Start();
            channel.BasicAcks += (sender, args) =>
                acked = true;
            channel.BasicNacks += (sender, args) =>
                nacked = true;
            channel.BasicReturn += (sender, args) => deliveryException = new DeliveryException(args.ReplyText) { RoutingKey = args.RoutingKey, ReplyCode = args.ReplyCode };
            channel.ConfirmSelect();
            //////cancellationToken.ThrowIfCancellationRequested();
            ////channel.BasicPublish(_options.Exchange, routingKey: message.Topic, basicProperties: null, mandatory: true, body: message.Payload);

            channel.WaitForConfirmsOrDie(_options.PublishTimeout);
        }
        catch (BrokerUnreachableException ex)
        {
            sw.Stop();
            sw1.Stop();
           throw new TimeoutException(TimeoutException.CannotConnectMessage, ex);
        }
        catch (Exception ex)
        {
            sw.Stop();
            sw1.Stop();
            throw new RepositoryException("Generic broker error.", ex);
        }

        if (deliveryException != null)
        {
            throw deliveryException;
        }

        if (!confirmed)
        {
            throw new DeliveryException($"Message '{message.MessageId}' could not be confirmed.");
        }

        return Task.CompletedTask;
    }

    internal Task<byte[]?> ReadMessageAsync(string queueName)
    {
        ArgumentException.ThrowIfNullOrEmpty(queueName, nameof(queueName));

        try
        {
            using IModel channel = this.Connection.CreateModel();
            BasicGetResult result = channel.BasicGet(queueName, autoAck: true);
            return Task.FromResult(result?.Body.ToArray());
        }
        catch (Exception ex)
        {
            throw new RepositoryException("Generic broker error.", ex);
        }
    }

    internal async Task<IReadOnlyCollection<byte[]>> ReadBatchAsync(string queueName, int batchSize, TimeSpan readTime, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(queueName, nameof(queueName));

        List<byte[]> result = new(batchSize);
        try
        {
            using IModel channel = this.Connection.CreateModel();
            channel.BasicQos(prefetchSize: 0 /*not implemented by the client*/, (ushort)batchSize, global: false);

            EventingBasicConsumer consumer = new(channel);
            consumer.Received += (sender, args) =>
            {
                result.Add(args.Body.ToArray());
                channel.BasicAck(args.DeliveryTag, multiple: true); // positively acknowledge all deliveries up to this delivery tag
            };

            string consumerTag = channel.BasicConsume(queueName, autoAck: false, consumer);
            await Task.Delay(readTime, cancellationToken).ConfigureAwait(false);
            channel.BasicCancel(consumerTag);
        }
        catch (Exception ex)
        {
            throw new RepositoryException("Generic broker error.", ex);
        }

        return result;
    }

    internal async Task<IReadOnlyCollection<ReadOnlyMemory<byte>>> ReadAllAsync(string queueName, CancellationToken cancellationToken)
    {
        const int delayBetweenReads = 50;
        List<ReadOnlyMemory<byte>> result = new();
        while (!cancellationToken.IsCancellationRequested)
        {
            ReadOnlyMemory<byte>? messagePayload = await this.ReadMessageAsync(queueName);
            if (messagePayload != null)
            {
                result.Add(messagePayload.Value);
            }
            else
            {
                try
                {
                    await Task.Delay(millisecondsDelay: delayBetweenReads, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        return result;
    }

    #region IDisposable
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
    #endregion
}
