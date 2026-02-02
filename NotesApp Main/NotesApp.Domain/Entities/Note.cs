using System;

namespace NotesApp.Domain.Entities
{
    public class Note
    {
        public Guid Id { get; set; }

        // Owner
        public Guid UserId { get; set; }

        // Core
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public string? BackgroundColor { get; set; }

        // For list/search (safe)
        public string? Summary { get; set; }

        // Attachments
        public string? FilePath { get; set; }
        public string? ImagePath { get; set; }

        // Reminder
        public DateTime? ReminderAt { get; set; }

        // 🔐 Protection
        public bool IsPasswordProtected { get; set; }
        public string? PasswordHash { get; set; }


        // Soft delete
        public bool IsDeleted { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? SummaryUpdatedAt { get; set; }
    }
}
