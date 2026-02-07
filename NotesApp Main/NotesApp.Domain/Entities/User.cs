namespace NotesApp.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    // ---------------- BASIC INFO ----------------
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? UserName { get; set; }
    public string? Email { get; set; }

    // ---------------- AUTH ----------------
    public string? PasswordHash { get; set; }
    public bool IsGuest { get; set; }

    // Google register/login flag
    public bool IsRegisteredWithGoogle { get; set; }

    // ---------------- PROFILE ----------------
    public string ProfileImagePath { get; set; } = "profile-images/default.png";

    public bool IsDeleted { get; set; }

    // ONE TIME SET (protected notes password)
    public string? CommonPasswordHash { get; set; }

    //NEW: USER-LEVEL ACCESS WINDOW (ALL protected notes)
    public DateTime? AccessibleFrom { get; set; }
    public DateTime? AccessibleTill { get; set; }

    // ---------------- PASSWORD RESET ----------------
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // ---------------- AUDIT ----------------
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ---------------- RELATIONSHIPS ----------------
    public virtual ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();
}
