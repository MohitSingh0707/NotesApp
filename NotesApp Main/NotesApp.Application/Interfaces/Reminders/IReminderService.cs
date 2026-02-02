using NotesApp.Application.DTOs.Reminders;
using System;
using System.Threading.Tasks;

namespace NotesApp.Application.Interfaces.Reminders
{
    public interface IReminderService
    {
        Task<Guid> CreateAsync(Guid userId, CreateReminderRequest request);
    }
}
