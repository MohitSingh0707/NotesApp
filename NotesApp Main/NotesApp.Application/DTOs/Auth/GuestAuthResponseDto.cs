namespace NotesApp.Application.DTOs.Auth;

public class GuestAuthResponseDto
{
    public Guid UserId { get; set; }
    public bool IsGuest { get; set; }
    public string Token { get; set; } = null!;
    public string ProfileImageUrl { get; set; } = null!;
    // 🔥 ACCESS WINDOW (FOR PROTECTED NOTES)
    public bool IsNotesUnlocked { get; set; }
    public long RemainingAccessSeconds { get; set; }
    public bool HasPushToken { get; set; }
}
