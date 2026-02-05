using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using AISummaryService.Models;

namespace AISummaryService.Rabbit;

public class SummaryRequestConsumer
{
    public async Task StartAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = "172.26.96.1",
            UserName = "guest",
            Password = "guest"
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "ai-summary-request",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (sender, args) =>
        {
            var body = args.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            var request = JsonSerializer.Deserialize<SummaryRequest>(json);

            Console.WriteLine($"📥 Received note {request?.NoteId}");

            // 🔥 For now, just acknowledge
            await channel.BasicAckAsync(args.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(
            queue: "ai-summary-request",
            autoAck: false,
            consumer: consumer);
    }
}
