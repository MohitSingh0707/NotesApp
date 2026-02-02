using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApp.Application.DTOs.Reminders;
using NotesApp.Application.Interfaces.Reminders;
using NotesApp.Application.Common.Extensions;
using NotesApp.Domain.Entities;
using Hangfire;
using NotesApp.Infrastructure.BackgroundJobs;
using NotesApp.Application.Interfaces.Notes;
using NotesApp.Application.Common;
using System;
using System.Threading.Tasks;
using NotesApp.Domain.Enums;
using NotesApp.Application.DTOs.Reminders;

namespace NotesApp.API.Controllers.Reminders
{
    [Authorize]
    [ApiController]
    [Route("api/reminders")]
    public class RemindersController : ControllerBase
    {
        private readonly IReminderRepository _reminderRepository;
        private readonly INoteService _noteService;

        public RemindersController(
            IReminderRepository reminderRepository,
            INoteService noteService)
        {
            _reminderRepository = reminderRepository;
            _noteService = noteService;
        }

        // SET / UPDATE REMINDER
        [HttpPost]
        public async Task<IActionResult> SetReminder([FromBody] SetReminderRequest request)
        {
            if (request == null)
            {
                return BadRequest(FailureResponse.Create<object>(
                    message: "Request body is required",
                    statusCode: 400
                ));
            }

            var userId = User.GetUserId();

            // SYSTEM TIME (local)
            var reminderTime = DateTime.SpecifyKind(
                request.RemindAt,
                DateTimeKind.Local
            );

            if (reminderTime < DateTime.Now)
            {
                return BadRequest(FailureResponse.Create<object>(
                    message: "Reminder time cannot be in the past",
                    statusCode: 400
                ));
            }

            // 🔍 Fetch note (mandatory for reminder)
            var note = await _noteService.GetByIdAsync(request.NoteId, userId, null);
            if (note == null)
            {
                return NotFound(FailureResponse.Create<object>(
                    message: "Note not found",
                    statusCode: 404
                ));
            }

            var reminder = await _reminderRepository
                .GetByNoteIdAsync(request.NoteId, userId);

            if (reminder == null)
            {
                reminder = new Reminder
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NoteId = request.NoteId,
                    RemindAt = reminderTime,
                    Title = request.Title,
                    Description = request.Description,
                    Type = request.Type,
                    CreatedAt = DateTime.UtcNow,
                    IsCompleted = false
                };

                await _reminderRepository.AddAsync(reminder);
            }
            else
            {
                reminder.RemindAt = reminderTime;
                reminder.Title = request.Title;
                reminder.Description = request.Description;
                reminder.Type = request.Type;
                reminder.UpdatedAt = DateTime.UtcNow; // Step 24: Entity has `UpdatedAt`.
                reminder.IsCompleted = false;
                reminder.IsCancelled = false;

                await _reminderRepository.UpdateAsync(reminder);
            }

            // Schedule Hangfire job
            
            //  Cancel existing job if any
            if (reminder != null && !string.IsNullOrEmpty(reminder.JobId))
            {
                BackgroundJob.Delete(reminder.JobId);
            }

            var userEmail = User.GetEmail();

            var jobId = BackgroundJob.Schedule<ReminderJob>(
                job => job.SendReminderAsync(
                    userId,
                    userEmail,
                    note.Id,
                    note.Title,
                    request.Type
                ),
                reminderTime
            );
            
            reminder.JobId = jobId;
            await _reminderRepository.UpdateAsync(reminder);

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: "Reminder saved successfully"
            ));
        }

        //  DELETE REMINDER
        [HttpDelete("{noteId:guid}")]
        public async Task<IActionResult> DeleteReminder(Guid noteId)
        {
            var userId = User.GetUserId();

            var reminder = await _reminderRepository
                .GetByNoteIdAsync(noteId, userId);

            if (reminder == null)
            {
                return NotFound(FailureResponse.Create<object>(
                    message: "Reminder not found",
                    statusCode: 404
                ));
            }

            // If a job exists, we should delete it from Hangfire (optional but good practice)
            if (!string.IsNullOrEmpty(reminder.JobId))
            {
                BackgroundJob.Delete(reminder.JobId);
            }

            await _reminderRepository.DeleteByNoteIdAsync(noteId, userId);

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: "Reminder deleted successfully"
            ));
        }
        //  GET REMINDER BY NOTE ID
        [HttpGet("{noteId:guid}")]
        public async Task<IActionResult> GetReminder(Guid noteId)
        {
            var userId = User.GetUserId();

            var reminder = await _reminderRepository
                .GetByNoteIdAsync(noteId, userId);

            if (reminder == null)
            {
                return NotFound(FailureResponse.Create<object>(
                    message: "Reminder not found for this note",
                    statusCode: 404
                ));
            }

            var response = new ReminderResponse
            {
                Id = reminder.Id,
                NoteId = (Guid)reminder.NoteId!,
                Title = reminder.Title,
                Description = reminder.Description,
                RemindAt = reminder.RemindAt,
                Type = reminder.Type,
                IsCompleted = reminder.IsCompleted,
                CreatedAt = reminder.CreatedAt
            };

            return Ok(SuccessResponse.Create<ReminderResponse>(
                data: response,
                message: "Reminder fetched successfully"
            ));
        }
    }
}
