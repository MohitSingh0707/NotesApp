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

        // ================= GET BY ID (OPEN NOTE) =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNoteById(
            Guid id,
            [FromQuery] string? password)
        {
            var userId = GetUserId();

            var note = await _noteService.GetByIdAsync(
                id,
                userId,
                password
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
            var userId = GetUserId();

            var result = await _noteService.GetListAsync(
                userId,
                search,
                pageNumber,
                pageSize
            );

            return Ok(result);
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
            [FromQuery] string? password)
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
                password,
                userId
            );

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: "Note deleted successfully"
            ));
        }
        // =================Unlock by note id=================
        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockProtectedNoteById(
     Guid id,
     [FromBody] UnlockNoteRequest request)
        {
            var userId = GetUserId();

            await _noteService.UnlockProtectedNotesAsync(
                id,
                request.Password,
                request.UnlockMinutes,
                userId
            );

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: $"Protected notes unlocked for {request.UnlockMinutes} minutes"
            ));
        }

        // ================= LOCK NOTE =================
        [HttpPost("{id}/lock")]
        public async Task<IActionResult> LockNote(Guid id, [FromBody] LockNoteRequest request)
        {
            var userId = GetUserId();
            await _noteService.LockNoteAsync(id, request, userId);

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: request.IsPasswordProtected ? "Note locked successfully" : "Note unlocked successfully"
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

