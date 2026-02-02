using System;

namespace NotesApp.Application.DTOs.Notes
{
    public class NoteResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public string? FilePath { get; set; }
        public string? ImagePath { get; set; }
        public string? BackgroundColor { get; set; }

        public bool IsPasswordProtected { get; set; }
        public bool IsLockedByTime { get; set; }
    }
}
