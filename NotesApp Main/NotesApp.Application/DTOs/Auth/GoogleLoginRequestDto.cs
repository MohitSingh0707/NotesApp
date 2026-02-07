using System.ComponentModel.DataAnnotations;

namespace NotesApp.Application.DTOs.Auth;

public class GoogleLoginRequestDto
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
    public string? FcmToken { get; set; }
    public string? Platform { get; set; }
}
