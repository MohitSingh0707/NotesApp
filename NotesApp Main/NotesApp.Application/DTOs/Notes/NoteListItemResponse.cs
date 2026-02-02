using System;

namespace NotesApp.Application.DTOs.Notes
{
    public class NoteListItemResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;

        // 🔒 List me content kabhi bhi nahi aayega
        public string? Content { get; set; }
        public string? BackgroundColor { get; set; }

        public bool IsPasswordProtected { get; set; }

        public bool IsReminderSet { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
