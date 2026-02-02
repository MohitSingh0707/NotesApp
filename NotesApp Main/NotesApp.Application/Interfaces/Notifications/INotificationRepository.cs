using NotesApp.Domain.Entities;

namespace NotesApp.Application.Interfaces.Notifications
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task<List<Notification>> GetByUserAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId, Guid userId);
    }
}
