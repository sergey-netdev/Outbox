namespace Outbox.Publisher.RabbitMQ;

public class OutboxPublisherOptions
{
    public const string DefaultSectionName = "RabbitMQ";

    public string? HostName { get; set; }

    public int Port { get; set; }

    public string? Exchange { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public TimeSpan PublishTimeout { get; set; }

    public TimeSpan ContinuationTimeout { get; set; }

    public TimeSpan HandshakeContinuationTimeout { get; set;}

    public TimeSpan RequestedConnectionTimeout { get; set; }

    public TimeSpan SocketReadTimeout { get; set; }
    
    public TimeSpan SocketWriteTimeout { get; set; }

    public TimeSpan RequestedHeartbeat { get; set;}
}
