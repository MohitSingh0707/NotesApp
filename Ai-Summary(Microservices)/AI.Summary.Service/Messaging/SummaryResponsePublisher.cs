using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using AISummaryService.Models;

namespace AISummaryService.Messaging;

public class SummaryResponsePublisher
{
    private IChannel? _channel;

    // ❌ constructor me async kaam nahi
    public SummaryResponsePublisher()
    {
    }

    // ✅ proper async initialization
    public async Task InitializeAsync(IConnection connection)
    {
        _channel = await connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: "ai-summary-response",
            durable: true,
            exclusive: false,
            autoDelete: false
        );
    }

    // ✅ async publish
    public async Task PublishAsync(SummaryResponse response)
    {
        var json = JsonSerializer.Serialize(response);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: "ai-summary-response",
            body: body
        );
    }
}
