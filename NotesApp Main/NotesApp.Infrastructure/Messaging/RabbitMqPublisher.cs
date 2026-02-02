using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace NotesApp.Infrastructure.Messaging;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly RabbitMqOptions _options;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task PublishAsync<T>(string queueName, T message)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            UserName = _options.Username,
            Password = _options.Password
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            body: body);
    }
}
