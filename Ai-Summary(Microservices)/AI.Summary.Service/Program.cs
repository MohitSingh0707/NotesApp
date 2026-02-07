using AISummaryService.Messaging;
using AISummaryService.Services;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var aiService = new OpenAiSummaryService(
    config["AI:ApiKey"]!,
    config["AI:Model"]!
);

// async RabbitMQ connection
var connection = await RabbitMqConnection.CreateAsync(config);

// async consumer startup
var consumer = new SummaryRequestConsumer(aiService);
await consumer.StartAsync(connection);

Console.WriteLine("🚀 AI Summary Service is listening...");
await Task.Delay(Timeout.Infinite);
