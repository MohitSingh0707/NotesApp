namespace AISummaryService.Models;

public class SummaryResponse
{
    public Guid NoteId { get; set; }
    public string Summary { get; set; } = string.Empty;
}
