namespace Outbox.Publisher.RabbitMQ;
using Outbox.Core;

[Serializable]
public class DeliveryException : RepositoryException
{
    public DeliveryException()
    {
    }

    public DeliveryException(string message)
        : base(message)
    {
    }

    public DeliveryException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public string? RoutingKey { get; set; }

    public int ReplyCode { get; set; }
}
