using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FluentValidation.AspNetCore;
using FluentValidation;
using NotesApp.Infrastructure;
using NotesApp.Application.Validators.Auth;
using NotesApp.Application.Validators.Notes;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NotesApp.API.Middleware;
using Hangfire;
using Hangfire.SqlServer;
using NotesApp.Infrastructure.SignalR.Hubs;

using Microsoft.AspNetCore.SignalR;
using NotesApp.API.SignalR;
using NotesApp.Application.Interfaces.Notifications;
using RabbitMQ.Client;
using NotesApp.Application.Interfaces.Emails;
using NotesApp.Application.Interfaces.Reminders;
using NotesApp.Application.Services.Reminders;
using NotesApp.Infrastructure.Persistence.Repositories.Reminders;
using NotesApp.Infrastructure.Persistence.Repositories.Reminders;
using NotesApp.Application.Interfaces.Common;
using NotesApp.Infrastructure.Persistence.Repositories;


var builder = WebApplication.CreateBuilder(args);

// --------------------
// Add Services
// --------------------

builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();

// Controllers
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateNoteRequestValidator>();

// Infrastructure (DB, Repos, Services, RabbitMQ, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// builder.Services.AddScoped<INotificationHub, NotificationHubService>();
builder.Services.AddScoped<IReminderRepository, ReminderRepository>();
builder.Services.AddScoped<IReminderService, ReminderService>();
// builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();




// --------------------
// Hangfire
// --------------------
builder.Services.AddHangfire(config =>
{
    config.UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15)
        });
});

builder.Services.AddHangfireServer();

// --------------------
// Swagger
// --------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------
// SignalR
// --------------------
builder.Services.AddSignalR();

// --------------------
// JWT Authentication
// --------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,

        // 🔥 SABSE IMPORTANT
        ValidateLifetime = false,   

        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey!)
        )
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/notificationHub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});


builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ValidationErrorResponse.Create;
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// --------------------
// Middleware Pipeline
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// --------------------
// Hangfire Dashboard
// --------------------
app.UseHangfireDashboard("/hangfire");

// --------------------
// SignalR Hub
// --------------------
app.MapHub<NotificationHub>("/notificationHub");


// --------------------
// Controllers
// --------------------
app.MapControllers();


// =====================================================
// 🔥 AI SUMMARY RESPONSE CONSUMER (FIXED PART)
// =====================================================

// ❌ DO NOT create DbContext here
// ❌ DO NOT use CreateScope() here

var rabbitConnection = app.Services.GetRequiredService<IConnection>();
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

var aiSummaryConsumer = new AiSummaryResponseConsumer(scopeFactory);
await aiSummaryConsumer.StartAsync(rabbitConnection);

Console.WriteLine("✅ AI Summary Response Consumer started");

// =====================================================

app.Run();
