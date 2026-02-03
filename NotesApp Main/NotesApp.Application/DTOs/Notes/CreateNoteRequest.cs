using System;
using System.ComponentModel.DataAnnotations;

namespace NotesApp.Application.DTOs.Notes
{
    public class CreateNoteRequest
    {
        
        public string? Title { get; set; }

        public string? Content { get; set; }

        // File/Image paths
        public List<string>? FilePaths { get; set; }
        public List<string>? ImagePaths { get; set; }

        public bool IsPasswordProtected { get; set; }

        // 🔥 ONLY FIRST TIME frontend bhejega
        public string? Password { get; set; }

        public string? BackgroundColor { get; set; }

        public DateTime? AccessibleFrom { get; set; }
        public DateTime? AccessibleTill { get; set; }
    }
}
