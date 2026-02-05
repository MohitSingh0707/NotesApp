using Microsoft.EntityFrameworkCore;
using NotesApp.Application.Interfaces.Reminders;
using NotesApp.Domain.Entities;
using NotesApp.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NotesApp.Infrastructure.Persistence.Repositories.Reminders
{
    public class ReminderRepository : IReminderRepository
    {
        private readonly AppDbContext _context;

        public ReminderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Reminder reminder)
        {
            await _context.Reminders.AddAsync(reminder);
            await _context.SaveChangesAsync();
        }

        public async Task<Reminder?> GetByIdAsync(Guid id)
        {
            return await _context.Reminders
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Reminder>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Reminders
                .Where(r => r.UserId == userId && !r.IsCancelled)
                .OrderBy(r => r.RemindAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Reminder reminder)
        {
            _context.Reminders.Update(reminder);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByNoteIdAsync(Guid noteId, Guid userId)
        {
            var reminder = await _context.Reminders
                .FirstOrDefaultAsync(r => r.NoteId == noteId && r.UserId == userId);

            if (reminder != null)
            {
                _context.Reminders.Remove(reminder);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Reminder?> GetByNoteIdAsync(Guid noteId, Guid userId)
        {
            return await _context.Reminders
                .FirstOrDefaultAsync(r => r.NoteId == noteId && r.UserId == userId);
        }

        public async Task DeleteAllByUserAsync(Guid userId)
        {
            var reminders = await _context.Reminders
                .Where(r => r.UserId == userId)
                .ToListAsync();

            if (reminders.Any())
            {
                _context.Reminders.RemoveRange(reminders);
                await _context.SaveChangesAsync();
            }
        }
    }
}
