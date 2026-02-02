namespace NotesApp.Application.DTOs.Push
{
    public class RegisterDeviceTokenRequest
    {
        public string Token { get; set; } = null!;
        public string Platform { get; set; } = "web"; 
        // web / android / ios
    }
}
