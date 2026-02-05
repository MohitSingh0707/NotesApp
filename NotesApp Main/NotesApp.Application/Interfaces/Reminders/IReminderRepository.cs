using NotesApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NotesApp.Application.Interfaces.Reminders
{
    public interface IReminderRepository
    {
        Task AddAsync(Reminder reminder);
        Task<Reminder?> GetByIdAsync(Guid id);
        Task<List<Reminder>> GetByUserIdAsync(Guid userId);
        Task UpdateAsync(Reminder reminder);
        Task DeleteByNoteIdAsync(Guid noteId, Guid userId);
        Task<Reminder?> GetByNoteIdAsync(Guid noteId, Guid userId);
        Task DeleteAllByUserAsync(Guid userId);
    }
}
