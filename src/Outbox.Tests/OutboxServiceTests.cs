﻿namespace Outbox.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Outbox.Core;
using Outbox.Publisher.RabbitMQ;
using Outbox.Service;
using Outbox.Sql;

public class OutboxServiceTests : TestBase
{
    private readonly OutboxService _service;
    private readonly IHost _host;

    public OutboxServiceTests()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddOutboxSqlRepository();
                services.AddOutboxRabbitMQPublisher();
                services.AddOutboxService();
            })
            .Build();

        _service = (OutboxService)_host.Services.GetRequiredService<IOutboxService>();
    }
}