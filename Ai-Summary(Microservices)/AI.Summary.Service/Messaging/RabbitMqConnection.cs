using RabbitMQ.Client;

namespace AISummaryService.Messaging;

public static class RabbitMqConnection
{
    public static async Task<IConnection> CreateAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost"
        };

        return await factory.CreateConnectionAsync();
    }
}
