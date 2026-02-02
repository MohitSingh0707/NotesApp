using System;
using System.Threading.Tasks;

namespace NotesApp.Application.Interfaces.Notifications
{
    public interface INotificationHub
    {
        Task SendAsync(Guid userId, string title, string message);
        Task SendReminderAsync(Guid userId, string title, string message);
    }
}
