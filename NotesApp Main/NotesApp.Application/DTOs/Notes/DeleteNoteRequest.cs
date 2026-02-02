namespace NotesApp.Application.DTOs.Notes
{
    public class DeleteNoteRequest
    {
        // Required only if note is password protected
        public string? Password { get; set; }
    }
}
