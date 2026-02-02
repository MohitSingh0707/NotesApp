using System;
using System.Threading.Tasks;

namespace NotesApp.Application.Interfaces.Reminders
{
    public interface IReminderJob
    {
        Task ExecuteAsync(Guid reminderId);
    }
}
