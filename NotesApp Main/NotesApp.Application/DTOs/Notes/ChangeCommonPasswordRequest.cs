namespace NotesApp.Application.DTOs.Auth
{
    public class ChangeCommonPasswordRequest
    {
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
