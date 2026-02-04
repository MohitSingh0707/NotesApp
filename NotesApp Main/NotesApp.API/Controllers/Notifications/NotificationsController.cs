using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApp.Application.Common;
using NotesApp.Application.Common.Extensions;
using NotesApp.Application.Interfaces.Notifications;

namespace NotesApp.API.Controllers.Notifications
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationsController(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        // 1️ GET all notifications (Bell click)
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.GetUserId();

            var notifications =
                await _notificationRepository.GetByUserAsync(userId);

            var response = notifications.Select(n =>
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

            var unreadCount = notifications.Count(n => !n.IsRead);

            var result = new
            {
                notifications = response,
                unreadCount = unreadCount
            };

            return Ok(SuccessResponse.Create(
                data: result,
                message: "Notifications fetched successfully"
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
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = User.GetUserId();

            var notifications = await _notificationRepository.GetByUserAsync(userId);
            
            // Filter in memory for now using existing repo method
            var unreadNotifications = notifications
                .Where(n => !n.IsRead)
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

            var unreadCount = unreadNotifications.Count;

            var result = new
            {
                notifications = unreadNotifications,
                unreadCount = unreadCount
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

            await _notificationRepository.MarkAsReadAsync(id, userId);

            // Fetch updated count
            var notifications = await _notificationRepository.GetByUserAsync(userId);
            var unreadCount = notifications.Count(n => !n.IsRead);

            return Ok(SuccessResponse.Create(
                data: new { unreadCount },
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
            }

            // Return updated count (should be 0 since all are marked as read)
            return Ok(SuccessResponse.Create(
                data: new { unreadCount = 0 },
                message: "All notifications marked as read"
            ));
        }
    }
}
