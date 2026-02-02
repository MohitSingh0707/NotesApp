using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using AISummaryService.Models;
using AISummaryService.Services;

namespace AISummaryService.Messaging;

public class SummaryRequestConsumer
{
    private readonly OpenAiSummaryService _aiService;
    private IChannel? _channel;
    private SummaryResponsePublisher? _publisher;

    // ❌ constructor me async kaam nahi
    public SummaryRequestConsumer(OpenAiSummaryService aiService)
    {
        _aiService = aiService;
    }

    // ✅ proper async startup
    public async Task StartAsync(IConnection connection)
    {
        _channel = await connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: "ai-summary-request",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        _publisher = new SummaryResponsePublisher();
        await _publisher.InitializeAsync(connection);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var request = JsonSerializer.Deserialize<SummaryRequest>(body)!;

            Console.WriteLine($"📥 Received note {request.NoteId}");

            var summary = await _aiService.GenerateSummaryAsync(request.Content);

            await _publisher.PublishAsync(new SummaryResponse
            {
                NoteId = request.NoteId,
                Summary = summary
            });

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync(
            queue: "ai-summary-request",
            autoAck: false,
            consumer: consumer
        );
    }
}
