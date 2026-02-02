using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace NotesApp.Infrastructure.Messaging;

public class AiSummaryRequestPublisher
{
    private readonly IConnection _connection;
    private IChannel? _channel;

    public AiSummaryRequestPublisher(IConnection connection)
    {
        _connection = connection;
    }

    public async Task InitializeAsync()
    {
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: "ai-summary-request",
            durable: true,
            exclusive: false,
            autoDelete: false
        );
    }

    public async Task PublishAsync(Guid noteId, string content)
    {
        var payload = new
        {
            NoteId = noteId,
            Content = content
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: "ai-summary-request",
            body: body
        );
    }
}
