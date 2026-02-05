using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApp.Application.Common;
using NotesApp.Application.Common.Extensions;
using NotesApp.Application.Interfaces.Notifications;
using NotesApp.Application.Interfaces.Reminders;
using NotesApp.Application.Interfaces.Notes;

namespace NotesApp.API.Controllers.Notifications
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IReminderRepository _reminderRepository;
        private readonly INoteService _noteService;

        public NotificationsController(
            INotificationRepository notificationRepository,
            IReminderRepository reminderRepository,
            INoteService noteService)
        {
            _notificationRepository = notificationRepository;
            _reminderRepository = reminderRepository;
            _noteService = noteService;
        }

        // 1️ GET all notifications (Bell click)
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            // Validate pagination
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1 || pageSize > 50)
                pageSize = 20;

            var userId = User.GetUserId();

            var allNotifications =
                await _notificationRepository.GetByUserAsync(userId);

            var unreadCount = allNotifications.Count(n => !n.IsRead);

            // Paginate
            var paginatedNotifications = allNotifications
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n =>
                    new NotificationResponseDto
                    {
                        Id = n.Id,
                        NoteId = n.NoteId,
                        NoteTitle = n.NoteTitle,
                        Title = n.Title,
                        Message = n.Message,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt
                    }).ToList();

            var result = new
            {
                notifications = paginatedNotifications,
                unreadCount = unreadCount
            };

            return Ok(SuccessResponse.Create(
                data: result,
                message: "Notifications fetched successfully"
            ));
        }

        // 1.5️ GET Single Notification by ID
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetNotificationById(Guid id)
        {
            var userId = User.GetUserId();

            var allNotifications = await _notificationRepository.GetByUserAsync(userId);
            var notification = allNotifications.FirstOrDefault(n => n.Id == id);

            if (notification == null)
            {
                return NotFound(FailureResponse.Create<object>(
                    message: "Notification not found",
                    statusCode: 404
                ));
            }

            // Build response with notification details
            var response = new
            {
                id = notification.Id,
                noteId = notification.NoteId,
                noteTitle = notification.NoteTitle,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt,
                reminder = (object?)null // Will be populated if noteId exists
            };

            // Fetch reminder details if notification has a NoteId
            if (notification.NoteId.HasValue)
            {
                var reminder = await _reminderRepository.GetByNoteIdAsync(notification.NoteId.Value, userId);
                if (reminder != null)
                {
                    response = response with
                    {
                        reminder = new
                        {
                            reminderTitle = reminder.Title,
                            reminderDescription = reminder.Description,
                            reminderTime = reminder.RemindAt,
                            reminderType = reminder.Type,
                            isCompleted = reminder.IsCompleted
                        }
                    };
                }
            }

            return Ok(SuccessResponse.Create(
                data: response,
                message: "Notification fetched successfully"
            ));
        }

        // 2️ GET unread count (Bell badge)
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.GetUserId();

            var notifications =
                await _notificationRepository.GetByUserAsync(userId);

            var unreadCount = notifications.Count(n => !n.IsRead);

            return Ok(SuccessResponse.Create(
                data: new { unreadCount },
                message: "Unread notification count fetched successfully"
            ));
        }

        // 2.5 GET unread notifications list
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadNotifications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            // Validate pagination
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1 || pageSize > 50)
                pageSize = 20;

            var userId = User.GetUserId();

            var notifications = await _notificationRepository.GetByUserAsync(userId);
            
            // Filter unread in memory for now
            var unreadList = notifications.Where(n => !n.IsRead).ToList();
            var totalUnread = unreadList.Count;

            // Paginate
            var paginatedUnread = unreadList
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    NoteId = n.NoteId,
                    NoteTitle = n.NoteTitle,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList();


            var result = new
            {
                notifications = paginatedUnread,
                unreadCount = totalUnread
            };

            return Ok(SuccessResponse.Create(
                data: result,
                message: "Unread notifications fetched successfully"
            ));
        }

        [HttpPut("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = User.GetUserId();

            // Get the notification to fetch NoteId
            var allNotifications = await _notificationRepository.GetByUserAsync(userId);
            var notification = allNotifications.FirstOrDefault(n => n.Id == id);

            if (notification == null)
            {
                return NotFound(FailureResponse.Create<object>(
                    message: "Notification not found",
                    statusCode: 404
                ));
            }

            // Mark as read
            await _notificationRepository.MarkAsReadAsync(id, userId);

            // Fetch updated count
            var unreadCount = allNotifications.Count(n => !n.IsRead && n.Id != id);

            // Fetch reminder details if notification has a NoteId
            object? reminderDetails = null;
            if (notification.NoteId.HasValue)
            {
                var reminder = await _reminderRepository.GetByNoteIdAsync(notification.NoteId.Value, userId);
                if (reminder != null)
                {
                    // 🔥 Mark reminder as completed
                    reminder.IsCompleted = true;
                    await _reminderRepository.UpdateAsync(reminder);

                    reminderDetails = new
                    {
                        reminderTitle = reminder.Title,
                        reminderDescription = reminder.Description,
                        reminderTime = reminder.RemindAt,
                        reminderType = reminder.Type,
                        isCompleted = reminder.IsCompleted,
                        noteTitle = notification.NoteTitle,
                        noteId = notification.NoteId
                    };
                }
            }

            return Ok(SuccessResponse.Create(
                data: new 
                { 
                    unreadCount,
                    reminder = reminderDetails
                },
                message: "Notification marked as read"
            ));
        }

        // 4️ MARK ALL notifications as read
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.GetUserId();

            var notifications =
                await _notificationRepository.GetByUserAsync(userId);

            foreach (var notification in notifications.Where(n => !n.IsRead))
            {
                await _notificationRepository.MarkAsReadAsync(notification.Id, userId);

                // 🔥 Mark associated reminders as completed 
                if (notification.NoteId.HasValue)
                {
                    var reminder = await _reminderRepository.GetByNoteIdAsync(notification.NoteId.Value, userId);
                    if (reminder != null)
                    {
                        reminder.IsCompleted = true;
                        await _reminderRepository.UpdateAsync(reminder);
                    }
                }
            }

            // Return updated count (should be 0 since all are marked as read)
            return Ok(SuccessResponse.Create(
                data: new { unreadCount = 0 },
                message: "All notifications marked as read"
            ));
        }

        // 5️ DELETE ALL notifications (Clear All)
        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAll()
        {
            var userId = User.GetUserId();

            await _notificationRepository.DeleteAllByUserAsync(userId);

            return Ok(SuccessResponse.Create<object>(
                data: null,
                message: "All notifications deleted successfully"
            ));
        }
    }
}
