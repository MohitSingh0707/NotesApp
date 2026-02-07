using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace NotesApp.Application.DTOs.Notes
{
    public class CreateNoteRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public bool IsPasswordProtected { get; set; } = false;
        public DateTime? AccessibleFrom { get; set; }
        public DateTime? AccessibleTill { get; set; }
        public string? BackgroundColor { get; set; } = "#ffffff";
        public bool IsReminderSet { get; set; } = false;

        // Support for string paths (URLs or existing paths)
        public List<string>? FilePaths { get; set; }
        public List<string>? ImagePaths { get; set; }

        // Support for direct file uploads via FormData [FromForm]
        public List<IFormFile>? NewFiles { get; set; }
        public List<IFormFile>? NewImages { get; set; }

        public string? Password { get; set; }
    }
}
