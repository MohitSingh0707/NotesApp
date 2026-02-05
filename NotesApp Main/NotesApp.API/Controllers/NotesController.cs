using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using NotesApp.Application.DTOs.Notes;
using NotesApp.Application.Interfaces.Notes;
using NotesApp.Application.Common;
using NotesApp.Application.Common.Extensions;
using NotesApp.Application.Interfaces.Reminders;
using Hangfire;

namespace NotesApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly IReminderRepository _reminderRepository;

        public NotesController(
            INoteService noteService,
            IReminderRepository reminderRepository)
        {
            _noteService = noteService;
            _reminderRepository = reminderRepository;
        }

        // ================= HELPER =================
        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid or missing user id");

            return userId;
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> CreateNote([FromBody] CreateNoteRequest? request)
        {
            if (request == null)
            {
                return BadRequest(FailureResponse.Create<object>(
                    message: "Request body is required",
                    statusCode: 400,
                    errors: new List<string> { "Request body cannot be null" }
                ));
            }

            var userId = GetUserId();
            var noteId = await _noteService.CreateAsync(request, userId);

            return Ok(SuccessResponse.Create(
                data: new { noteId },
                message: "Note created successfully"
            ));
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNoteById(Guid id)
        {
            var userId = GetUserId();

            var note = await _noteService.GetByIdAsync(
                id,
                userId
            );

            return Ok(SuccessResponse.Create(
                data: note,
                message: "Note fetched successfully"
            ));
        }

        // ================= LIST =================
        [HttpGet]
        public async Task<IActionResult> GetNotes(
            [FromQuery] string? search,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            // Validate pagination parameters before calling service
            if (pageNumber < 1)
                return BadRequest(FailureResponse.Create<object>(
                    message: "Page number must be at least 1",
                    statusCode: 400));

            if (pageSize < 1 || pageSize > 100)
                return BadRequest(FailureResponse.Create<object>(
                    message: "Page size must be between 1 and 100",
                    statusCode: 400));

            var userId = GetUserId();

            var result = await _noteService.GetListAsync(
                userId,
                search,
                1, // Always first page for now if pagination field is removed
                1000 // Large page size to act as "all"
            );

            return Ok(SuccessResponse.Create(
                data: result.Items,
                message: "Notes fetched successfully"
            ));
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(
            Guid id,
            [FromBody] UpdateNoteRequest? request)
        {
            if (request == null)
            {
                return BadRequest(FailureResponse.Create<object>(
                    message: "Request body is required",
                    statusCode: 400,
                    errors: new List<string> { "Request body cannot be null" }
                ));
            }

            var userId = GetUserId();

            await _noteService.UpdateAsync(
                id,
                request,
                userId
            );

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: "Note updated successfully"
            ));
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(
            Guid id,
            [FromBody] DeleteNoteRequest? request)
        {
            var userId = GetUserId();

            // Cascade Cleanup: Cancel jobs and delete reminder associated with this note
            var reminder = await _reminderRepository.GetByNoteIdAsync(id, userId);
            if (reminder != null)
            {
                if (!string.IsNullOrEmpty(reminder.JobId))
                {
                    BackgroundJob.Delete(reminder.JobId);
                }
                await _reminderRepository.DeleteByNoteIdAsync(id, userId);
            }

            await _noteService.DeleteAsync(
                id,
                request?.Password,
                userId
            );

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: "Note deleted successfully"
            ));
        }
        // ================= UNLOCK (GLOBAL) =================
        [HttpPost("unlock")]
        public async Task<IActionResult> UnlockProtectedNotes([FromBody] UnlockNoteRequest request)
        {
            var userId = GetUserId();

            await _noteService.UnlockProtectedNotesAsync(
                request.Password,
                request.UnlockMinutes,
                userId
            );

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: $"Protected notes unlocked for {request.UnlockMinutes} minutes"
            ));
        }

        // ================= LOCK (GLOBAL) =================
        [HttpPost("lock")]
        public async Task<IActionResult> LockNotes()
        {
            var userId = GetUserId();
            await _noteService.LockProtectedNotesAsync(userId);

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: "All protected notes have been locked"
            ));
        }



        // ================= SUMMARY =================
        [HttpPost("{id}/generate-summary")]
        public async Task<IActionResult> GenerateSummary(Guid id)
        {
            await _noteService.GenerateSummaryAsync(id);

            for (int i = 0; i < 6; i++)
            {
                await Task.Delay(500);

                var note = await _noteService.GetNoteWithSummaryAsync(id);
                if (!string.IsNullOrWhiteSpace(note?.Summary))
                {
                    return Ok(new
                    {
                        summary = note.Summary,
                    });
                }
            }

            return Accepted(new
            {
                message = "Summary is being generated",
                status = "pending"
            });
        }
    }
}

