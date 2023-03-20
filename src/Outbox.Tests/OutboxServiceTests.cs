namespace Outbox.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Outbox.Service;
using Outbox.Sql;

public class OutboxServiceTests
{
    private readonly OutboxService _service;
    private readonly OutboxServiceOptions serviceOptions = new();
    private readonly OutboxRepositoryOptions repositoryOptions = new();

    public OutboxServiceTests()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        configuration.GetSection(OutboxServiceOptions.DefaultSectionName).Bind(serviceOptions);
        configuration.GetSection(OutboxRepositoryOptions.DefaultSectionName).Bind(repositoryOptions);

        var repository = new OutboxRepository(repositoryOptions);
        _service = new OutboxService(serviceOptions, repository, NullLogger<OutboxService>.Instance);
    }
}