namespace AISummaryService.Models;

public class SummaryRequest
{
    public Guid NoteId { get; set; }
    public string Content { get; set; } = string.Empty;
}
