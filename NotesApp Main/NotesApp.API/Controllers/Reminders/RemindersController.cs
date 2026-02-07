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
            Console.WriteLine($"🔔 API HIT: SetReminder for NoteId: {request.NoteId}, UserId: {userId}, RemindAt: {request.RemindAt:yyyy-MM-dd HH:mm:ss}Z, Type: {request.Type}");

            if (User.IsGuest())
            {
                return StatusCode(403, FailureResponse.Create<object>(
                    message: "Guest users cannot set reminders. Please register to use this feature.",
                    statusCode: 403
                ));
            }

            // 🔥 TRUST INCOMING UTC
            var reminderTime = DateTime.SpecifyKind(request.RemindAt, DateTimeKind.Utc);
            var now = DateTime.UtcNow;
            
            var timeRemaining = reminderTime - now;
            Console.WriteLine($"⏰ Scheduling: Requested={request.RemindAt}, UTC={reminderTime}, Now={now}, TRIGGER IN: {timeRemaining.TotalMinutes:F1} minutes");
            
            // Validate time is in the future (with buffer to account for network delay)
            if (reminderTime < now)
            {
                return BadRequest(FailureResponse.Create<object>(
                    message: "Reminder time cannot be in the past",
                    statusCode: 400,
                    errors: new List<string> { $"Requested time: {reminderTime:yyyy-MM-dd HH:mm:ss}, Current time: {now:yyyy-MM-dd HH:mm:ss}" }
                ));
            }

            // Add minimum 1 minute buffer
            if (reminderTime < now.AddMinutes(1))
            {
                return BadRequest(FailureResponse.Create<object>(
                    message: "Reminder must be at least 1 minute in the future",
                    statusCode: 400
                ));
            }

            // 🔍 Fetch note (mandatory for reminder)
            var note = await _noteService.GetByIdAsync(request.NoteId, userId);
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

            // Schedule Hangfire job with error handling
            try
            {
                //  Cancel existing job if any
                if (reminder != null && !string.IsNullOrEmpty(reminder.JobId))
                {
                    try
                    {
                        BackgroundJob.Delete(reminder.JobId);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail if old job deletion fails
                        Console.WriteLine($"Failed to delete old Hangfire job {reminder.JobId}: {ex.Message}");
                    }
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
                
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new Exception("Hangfire returned null/empty job ID");
                }

                Console.WriteLine($"✅ Hangfire job scheduled: {jobId} for {reminderTime}");
                reminder.JobId = jobId;
                await _reminderRepository.UpdateAsync(reminder);
            }
            catch (Exception ex)
            {
                // Rollback: delete the reminder if job scheduling failed
                if (reminder.CreatedAt == reminder.UpdatedAt) // Was just created
                {
                    await _reminderRepository.DeleteByNoteIdAsync(request.NoteId, userId);
                }
                
                return StatusCode(500, FailureResponse.Create<object>(
                    message: "Failed to schedule reminder",
                    statusCode: 500,
                    errors: new List<string> { $"Error: {ex.Message}" }
                ));
            }

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

            // If a job exists, we should delete it from Hangfire
            if (!string.IsNullOrEmpty(reminder.JobId))
            {
                try
                {
                    BackgroundJob.Delete(reminder.JobId);
                }
                catch (Exception ex)
                {
                    // Log but don't fail deletion if job cleanup fails
                    Console.WriteLine($"Failed to delete Hangfire job {reminder.JobId}: {ex.Message}");
                }
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

            // Validate NoteId is not null before casting
            if (!reminder.NoteId.HasValue)
            {
                return StatusCode(500, FailureResponse.Create<object>(
                    message: "Reminder data is corrupted (missing NoteId)",
                    statusCode: 500
                ));
            }

            var response = new ReminderResponse
            {
                Id = reminder.Id,
                NoteId = reminder.NoteId.Value,
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

        // CLEAR ALL REMINDERS (Delete all for user)
        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAllReminders()
        {
            var userId = User.GetUserId();

            // Get all reminders for this user
            var reminders = await _reminderRepository.GetByUserIdAsync(userId);
            
            if (reminders == null || !reminders.Any())
            {
                return Ok(SuccessResponse.Create<object>(
                    data: null,
                    message: "No reminders to clear"
                ));
            }

            var deletedCount = 0;
            var failedJobCancellations = 0;

            // Cancel Hangfire jobs and delete reminders
            foreach (var reminder in reminders)
            {
                if (!string.IsNullOrEmpty(reminder.JobId))
                {
                    try
                    {
                        BackgroundJob.Delete(reminder.JobId);
                    }
                    catch (Exception ex)
                    {
                        failedJobCancellations++;
                        Console.WriteLine($"Failed to delete Hangfire job {reminder.JobId}: {ex.Message}");
                    }
                }
                deletedCount++;
            }

            // Delete all reminders from database
            await _reminderRepository.DeleteAllByUserAsync(userId);

            var message = $"{deletedCount} reminder(s) deleted successfully";
            if (failedJobCancellations > 0)
                message += $" ({failedJobCancellations} job cancellation(s) failed but reminders deleted)";

            return Ok(SuccessResponse.Create(
                data: new { deletedCount, failedJobCancellations },
                message: message
            ));
        }
    }
}
