using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NotesApp.Infrastructure.Persistence;

public class AiSummaryResponseConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AiSummaryResponseConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(IConnection connection)
    {
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "ai-summary-response",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var data = JsonSerializer.Deserialize<SummaryResponse>(json)!;

            Console.WriteLine($"📬 Summary received for note {data.NoteId}");

            var note = await db.Notes
                .FirstOrDefaultAsync(n => n.Id == data.NoteId);

            if (note != null)
            {
                note.Summary = data.Summary;
                note.SummaryUpdatedAt = DateTime.UtcNow;
                note.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                Console.WriteLine("💾 Summary saved to DB");
            }
            else
            {
                Console.WriteLine("⚠️ Note not found in DB");
            }

            await channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(
            queue: "ai-summary-response",
            autoAck: false,
            consumer: consumer
        );
    }

    private record SummaryResponse(Guid NoteId, string Summary);
}
