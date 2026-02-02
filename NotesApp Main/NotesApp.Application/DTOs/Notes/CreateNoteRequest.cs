using System;
using System.ComponentModel.DataAnnotations;

namespace NotesApp.Application.DTOs.Notes
{
    public class CreateNoteRequest
    {
        
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        // File/Image paths (future use)
        public string? FilePath { get; set; }
        public string? ImagePath { get; set; }

        public bool IsPasswordProtected { get; set; }

        // 🔥 ONLY FIRST TIME frontend bhejega
        public string? Password { get; set; }


        public DateTime? AccessibleFrom { get; set; }
        public DateTime? AccessibleTill { get; set; }
    }
}
