using Amazon;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

using NotesApp.Application.Interfaces.Auth;
using NotesApp.Application.Interfaces.Common;
using NotesApp.Application.Interfaces.Notes;
using NotesApp.Application.Interfaces.Notifications;
using NotesApp.Application.Interfaces.Push;
using NotesApp.Application.Interfaces.Reminders;
using NotesApp.Application.Interfaces.Emails;
using NotesApp.Application.Interfaces.Users;
using NotesApp.Application.Interfaces.Files;

using NotesApp.Application.Services.Auth;
using NotesApp.Application.Services.Users;
using NotesApp.Infrastructure.Auth;
using NotesApp.Infrastructure.Messaging;
using NotesApp.Infrastructure.Persistence;
using NotesApp.Infrastructure.Persistence.Repositories;
using NotesApp.Infrastructure.Persistence.Repositories.Notifications;
using NotesApp.Infrastructure.Persistence.Repositories.Push;
using NotesApp.Infrastructure.Persistence.Repositories.Reminders;
using NotesApp.Infrastructure.Services.Notes;
using NotesApp.Infrastructure.BackgroundJobs;
using NotesApp.Infrastructure.Files;

namespace NotesApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ================= JWT =================
            var jwtOptions = new JwtOptions();
            configuration.GetSection("Jwt").Bind(jwtOptions);

            services.AddSingleton(jwtOptions);
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

            // ================= DATABASE =================
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")
                ));

            // ================= REPOSITORIES =================
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IReminderRepository, ReminderRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();

            // ================= APPLICATION SERVICES =================
            // ================= APPLICATION SERVICES =================
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<INoteService, NoteService>();
            services.AddScoped<IFileStorageService, S3FileStorageService>();

            // Added Missing Service
            services.AddScoped<INotificationService, NotesApp.Infrastructure.Notifications.NotificationService>();

            // ================= EMAIL =================
            // ================= EMAIL =================
            services.AddScoped<IEmailService, NotesApp.Infrastructure.Email.MailKitEmailService>();

            // ================= PUSH NOTIFICATIONS =================
            services.AddScoped<IPushNotificationService, NotesApp.Infrastructure.Push.FcmPushNotificationService>();

            // ================= REMINDER BACKGROUND JOB =================
            services.AddScoped<ReminderJob>();

            // ================= RABBIT MQ =================
            services.Configure<RabbitMqOptions>(
                configuration.GetSection("RabbitMQ"));

            services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

            services.AddSingleton<IConnection>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;
                var factory = new ConnectionFactory
                {
                    HostName = options.Host,
                    UserName = options.Username,
                    Password = options.Password
                };
                return factory.CreateConnectionAsync().Result;
            });

            services.AddSingleton<AiSummaryRequestPublisher>();

            // ================= GOOGLE AUTH =================
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();

            // ================= AWS S3 =================
            services.AddSingleton<IAmazonS3>(_ =>
                new AmazonS3Client(
                    configuration["AWS:AccessKey"],
                    configuration["AWS:SecretKey"],
                    RegionEndpoint.EUSouth1
                ));

            return services;
        }
    }
}
