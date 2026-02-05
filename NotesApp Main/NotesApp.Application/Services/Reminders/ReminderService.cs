using Hangfire;
using NotesApp.Application.DTOs.Reminders;
using NotesApp.Application.Interfaces.Reminders;
using NotesApp.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace NotesApp.Application.Services.Reminders
{
    public class ReminderService : IReminderService
    {
        private readonly IReminderRepository _repository;

        public ReminderService(IReminderRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> CreateAsync(Guid userId, CreateReminderRequest request)
        {
            if (request.RemindAt <= DateTime.Now)
                throw new Exception("Reminder time must be in the future");

            var reminder = new Reminder
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title,
                Description = request.Description,
                RemindAt = request.RemindAt.ToUniversalTime(),
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(reminder);

            // Schedule REAL reminder job
            var jobId = BackgroundJob.Schedule<IReminderJob>(
                job => job.ExecuteAsync(reminder.Id),
                reminder.RemindAt
            );

            reminder.JobId = jobId;
            await _repository.UpdateAsync(reminder);

            return reminder.Id;
        }
    }
}
