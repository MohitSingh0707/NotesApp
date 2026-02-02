using NotesApp.Application.Interfaces.Notifications;
using NotesApp.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace NotesApp.Infrastructure.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        // private readonly INotificationHub _notificationHub; // Hub dependency might be cyclic or unavailable, checking later

        public NotificationService(
            INotificationRepository notificationRepository
            // INotificationHub notificationHub
            )
        {
            _notificationRepository = notificationRepository;
            // _notificationHub = notificationHub;
        }

        public async Task CreateAsync(
            Guid userId,
            string title,
            string message,
            string type,
            Guid? noteId = null,
            string? noteTitle = null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,

                Title = title,
                Message = message,
                Type = type,

                NoteId = noteId,
                NoteTitle = noteTitle,

                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            // 1️⃣ Save to DB
            await _notificationRepository.AddAsync(notification);

            // 2️⃣ Real-time push 🔔 (Commented out for now to avoid dependency cycle if Hub uses Service)
            // await _notificationHub.PushAsync(userId, new
            // {
            //     notification.Id,
            //     notification.Title,
            //     notification.Message,
            //     notification.Type,
            //     notification.NoteId,
            //     notification.NoteTitle,
            //     notification.CreatedAt
            // });
        }
    }
}
