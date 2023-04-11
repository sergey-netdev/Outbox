namespace Outbox.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outbox.Core;
using Outbox.Sql;
using System;

public static class ConfigurationExtensions
{
    public static void AddOutboxSqlRepository(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        services.AddSingleton(serviceProvider =>
        {
            IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
            OutboxRepositoryOptions options = new();
            configuration.GetSection(OutboxRepositoryOptions.DefaultSectionName).Bind(options);
            return options;
        });

        services.AddSingleton<IOutboxRepository, OutboxRepository>();
    }
}
