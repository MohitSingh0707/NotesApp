using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace NotesApp.API.SignalR
{
    public class UserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // JWT claim se userId uthao
            // IMPORTANT: yahi wahi claim hona chahiye jo tum API me use karte ho
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
