namespace NotesApp.Application.DTOs.Notes
{
    public class UpdateNoteRequest
    {
        public string Title { get; set; } = null!;
        public string? Content { get; set; }

        public string? FilePath { get; set; }
        public string? ImagePath { get; set; }
        public string? BackgroundColor { get; set; }

        // 🔐 OPTIONAL: only send when you actually want to lock/unlock
        public bool? IsPasswordProtected { get; set; }

        // 🔑 Required ONLY when:
        // - updating a protected note without active unlock window
        // - OR when changing IsPasswordProtected value
        public string? Password { get; set; }

        // ⏱️ Optional: only used when unlocking protected notes
        public int? UnlockMinutes { get; set; }
    }
}
