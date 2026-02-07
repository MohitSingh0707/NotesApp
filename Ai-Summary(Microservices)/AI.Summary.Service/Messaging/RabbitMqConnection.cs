using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;

namespace AISummaryService.Messaging;

public static class RabbitMqConnection
{
    public static async Task<IConnection> CreateAsync(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        return await factory.CreateConnectionAsync();
    }
}
