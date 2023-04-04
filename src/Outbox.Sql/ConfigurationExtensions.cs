namespace Outbox.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outbox.Core;
using Outbox.Sql;

public static class ConfigurationExtensions
{
    public static void AddOutboxSqlRepository(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        OutboxRepositoryOptions options = new();
        configuration.GetSection(OutboxRepositoryOptions.DefaultSectionName).Bind(options);
        services.AddSingleton(options);

        services.AddSingleton<IOutboxRepository, OutboxRepository>();
    }
}
