using System;
using System.Security.Claims;

namespace NotesApp.Application.Common.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("UserId claim not found");

            return Guid.Parse(userId);
        }

        public static string GetEmail(this ClaimsPrincipal user)
        {
            var email =
                user.FindFirstValue(ClaimTypes.Email)
                ?? user.FindFirstValue("email"); // fallback for custom JWT

            if (string.IsNullOrWhiteSpace(email))
                throw new UnauthorizedAccessException("Email claim not found");

            return email;
        }
    }
}
