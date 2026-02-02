using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NotesApp.Infrastructure.SignalR.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
