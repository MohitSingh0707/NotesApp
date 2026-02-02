namespace NotesApp.Domain.Entities
{
    public class DeviceToken
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Token { get; set; } = null!;

        public string Platform { get; set; } = null!; 
        // web / android / ios

        public DateTime CreatedAt { get; set; }
    }
}
