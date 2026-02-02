using System;
using System.ComponentModel.DataAnnotations;
using NotesApp.Domain.Enums;

namespace NotesApp.Application.DTOs.Reminders
{
    public class SetReminderRequest
    {
        [Required]
        public Guid NoteId { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public DateTime RemindAt { get; set; } // User time

        public ReminderType Type { get; set; } = ReminderType.InApp | ReminderType.Email; // Default to InApp + Email if not specified
    }
}
