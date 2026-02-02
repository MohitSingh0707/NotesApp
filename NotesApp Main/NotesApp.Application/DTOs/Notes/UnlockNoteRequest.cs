public class UnlockNoteRequest
{
    public string Password { get; set; } = null!;
    public int UnlockMinutes { get; set; } // frontend se aayega
}
