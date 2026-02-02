using NotesApp.Domain.Enums;
using System;

namespace NotesApp.Application.DTOs.Reminders
{
    public class ReminderResponse
    {
        public Guid Id { get; set; }
        public Guid NoteId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime RemindAt { get; set; }
        public ReminderType Type { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
