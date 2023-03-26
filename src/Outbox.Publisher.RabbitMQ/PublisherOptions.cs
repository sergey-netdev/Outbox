namespace Outbox.Publisher.RabbitMQ;

public class PublisherOptions
{
    public const string DefaultSectionName = "RabbitMQ";

    public string? HostName { get; set; }

    public int Port { get; set; }

    public string? Exchange { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }
}
