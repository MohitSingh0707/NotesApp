using System;

namespace NotesApp.Application.DTOs.Notes
{
    public class NoteResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public List<string>? FilePaths { get; set; }
        public List<string>? ImagePaths { get; set; }
        public string? BackgroundColor { get; set; }

        public bool IsPasswordProtected { get; set; }
        public bool IsLockedByTime { get; set; }
        public bool IsReminderSet { get; set; }
    }
}
