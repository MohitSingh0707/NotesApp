using Microsoft.AspNetCore.SignalR;
using NotesApp.Application.Interfaces.Notifications;
using NotesApp.Infrastructure.SignalR.Hubs;
using System;
using System.Threading.Tasks;

namespace NotesApp.Infrastructure.SignalR
{
    public class NotificationHubService : INotificationHub
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationHubService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendAsync(Guid userId, string title, string message)
        {
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync("ReceiveNotification", new
                {
                    title,
                    message,
                    type = "General",
                    createdAt = DateTime.UtcNow
                });
        }

        public async Task SendReminderAsync(Guid userId, string title, string message)
        {
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync("ReceiveNotification", new
                {
                    title,
                    message,
                    type = "Reminder",
                    createdAt = DateTime.UtcNow
                });
        }
    }
}
