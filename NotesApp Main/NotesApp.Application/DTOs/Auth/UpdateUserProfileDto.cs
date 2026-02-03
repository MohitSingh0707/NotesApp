using System.ComponentModel.DataAnnotations;

namespace NotesApp.Application.DTOs.Auth;

public class UpdateUserProfileDto
{
    [StringLength(50, MinimumLength = 2)]
    public string? FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    public string? LastName { get; set; }

    [StringLength(20, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_\.]+$", ErrorMessage = "Username can only contain letters, numbers, underscores and dots")]
    public string? UserName { get; set; }

    public string? ProfileImagePath { get; set; }
}
