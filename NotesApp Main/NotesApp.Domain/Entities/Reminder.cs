using System;

namespace NotesApp.Domain.Entities
{
    public class Reminder
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? NoteId { get; set; }

        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public DateTime RemindAt { get; set; }   // UTC time
        public string? JobId { get; set; }       // Hangfire JobId
        
        public Enums.ReminderType Type { get; set; }

        public bool IsCompleted { get; set; }
        public bool IsCancelled { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
