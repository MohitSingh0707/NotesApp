using System;
using System.ComponentModel.DataAnnotations;

namespace NotesApp.Application.DTOs.Reminders
{
    public class CreateReminderRequest
    {
        [Required]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public DateTime RemindAt { get; set; } // User time
    }
}
