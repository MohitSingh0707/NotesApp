using System.ComponentModel.DataAnnotations;

namespace NotesApp.Application.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = null!;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = null!;

    [Required]
    [StringLength(20, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_\.]+$", ErrorMessage = "Username can only contain letters, numbers, underscores and dots")]
    public string UserName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number and one special character")]
    public string Password { get; set; } = null!;

    public string? FcmToken { get; set; }
    public string? Platform { get; set; }
}
