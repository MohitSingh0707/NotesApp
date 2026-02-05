using RabbitMQ.Client;

namespace AISummaryService.Messaging;

public static class RabbitMqConnection
{
    public static async Task<IConnection> CreateAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = "172.26.96.1"
        };

        return await factory.CreateConnectionAsync();
    }
}
