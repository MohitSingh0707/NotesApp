// // using AISummaryService.Rabbit;
// // using Microsoft.Extensions.Configuration;

// // Console.WriteLine("🚀 AI Summary Service started");
// // var config = new ConfigurationBuilder()
// //     .AddJsonFile("appsettings.json")
// //     .Build();

// // Console.WriteLine("🔑 OpenAI key loaded: " + 
// //     (!string.IsNullOrEmpty(config["OpenAI:ApiKey"])));

// // var consumer = new SummaryRequestConsumer();
// // await consumer.StartAsync();


// // await Task.Delay(Timeout.Infinite);

// using AISummaryService.Services;
// using Microsoft.Extensions.Configuration;

// var config = new ConfigurationBuilder()
//     .AddJsonFile("appsettings.json")
//     .Build();

// var openAi = new OpenAiSummaryService(
//     config["AI:ApiKey"]!,
//     config["AI:Model"]!
// );

// Console.WriteLine("🤖 Testing AI summary...");

// var testText = """
// Microservices architecture allows applications to be broken into smaller,
// independent services that communicate over messaging systems like RabbitMQ.
// This improves scalability and fault tolerance.
// """;

// var summary = await openAi.GenerateSummaryAsync(testText);

// Console.WriteLine("✅ SUMMARY GENERATED:");
// Console.WriteLine(summary);

// using AISummaryService.Messaging;
// using AISummaryService.Services;
// using Microsoft.Extensions.Configuration;

// var config = new ConfigurationBuilder()
//     .AddJsonFile("appsettings.json")
//     .Build();

// var aiService = new OpenAiSummaryService(
//     config["AI:ApiKey"]!,
//     config["AI:Model"]!
// );

// var connection = RabbitMqConnection.Create();

// var consumer = new SummaryRequestConsumer(connection, aiService);
// consumer.Start();

// Console.WriteLine("🚀 AI Summary Service is listening...");
// Console.ReadLine();

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

// ✅ async RabbitMQ connection
var connection = await RabbitMqConnection.CreateAsync(config);

// ✅ async consumer startup
var consumer = new SummaryRequestConsumer(aiService);
await consumer.StartAsync(connection);

Console.WriteLine("🚀 AI Summary Service is listening...");
await Task.Delay(Timeout.Infinite);
