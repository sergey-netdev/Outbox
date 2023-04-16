namespace Outbox.Publisher.RabbitMQ;
using Outbox.Core;

[Serializable]
public class TimeoutException : RepositoryException
{
    public const string CannotConnectMessage = "Could not access broker. Check the hostname, port, connect timeout.";
    public const string IntervalExceededMessage = "Exceeded {0} interval.";

    public TimeoutException()
    {
    }

    public TimeoutException(string message)
        : base(message)
    {
    }

    public TimeoutException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
