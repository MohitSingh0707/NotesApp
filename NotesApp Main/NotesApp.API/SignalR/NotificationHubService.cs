// using Microsoft.AspNetCore.SignalR;
// using NotesApp.API.Hubs;
// using NotesApp.Application.Interfaces.Notifications;

// namespace NotesApp.API.SignalR
// {
//     public class NotificationHubService : INotificationHub
//     {
//         private readonly IHubContext<NotificationHub> _hubContext;

//         public NotificationHubService(IHubContext<NotificationHub> hubContext)
//         {
//             _hubContext = hubContext;
//         }

//         public async Task PushAsync(Guid userId, object payload)
//         {
//             await _hubContext
//                 .Clients
//                 .User(userId.ToString())
//                 .SendAsync("ReceiveNotification", payload);
//         }
//     }
// }
