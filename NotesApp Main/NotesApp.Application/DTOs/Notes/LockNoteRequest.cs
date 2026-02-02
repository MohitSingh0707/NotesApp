public class LockNoteRequest
{
    public bool IsPasswordProtected { get; set; }

    public string? Password { get; set; }

    public DateTime? AccessibleFrom { get; set; }
    public DateTime? AccessibleTill { get; set; }
}
