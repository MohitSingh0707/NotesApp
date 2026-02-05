using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NotesApp.Application.DTOs.Notes;
using NotesApp.Application.Interfaces.Notes;
using NotesApp.Domain.Entities;
using NotesApp.Infrastructure.Persistence;
using NotesApp.Infrastructure.Messaging;
using NotesApp.Application.Common.Exceptions;
using System.ComponentModel.DataAnnotations;
using NotesApp.Application.DTOs.Common;

namespace NotesApp.Infrastructure.Services.Notes
{
    public class NoteService : INoteService
    {
        private readonly AppDbContext _context;
        private readonly AiSummaryRequestPublisher _aiPublisher;

        public NoteService(
            AppDbContext context,
            AiSummaryRequestPublisher aiPublisher)
        {
            _context = context;
            _aiPublisher = aiPublisher;
        }

        // ================= CLEANUP =================
        private async Task CleanupExpiredAccessAsync(User user)
        {
            if (user.AccessibleTill.HasValue &&
                DateTime.UtcNow > user.AccessibleTill.Value)
            {
                user.AccessibleFrom = null;
                user.AccessibleTill = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // ================= CREATE =================
        public async Task<Guid> CreateAsync(CreateNoteRequest request, Guid userId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new NotFoundException("User not found");

            // 🧹 SANITIZE & VALIDATE
            var cleanTitle = request.Title?.Trim();
            var cleanContent = request.Content?.Trim();

            // Validate array sizes
            if (request.FilePaths != null && request.FilePaths.Count > 10)
                throw new ValidationException("Maximum 10 files allowed per note");
            
            if (request.ImagePaths != null && request.ImagePaths.Count > 10)
                throw new ValidationException("Maximum 10 images allowed per note");

            // Validate URLs for file and image paths
            if (request.FilePaths != null)
            {
                foreach (var path in request.FilePaths)
                {
                    if (!IsValidS3Path(path))
                        throw new ValidationException($"Invalid file path: {path}");
                }
            }

            if (request.ImagePaths != null)
            {
                foreach (var path in request.ImagePaths)
                {
                    if (!IsValidS3Path(path))
                        throw new ValidationException($"Invalid image path: {path}");
                }
            }

            // XSS validation removed - allowing HTML content in notes
            
            // Track if truncation occurred
            bool wasTruncated = false;
            
            if (cleanTitle?.Length > 200)
            {
                cleanTitle = cleanTitle.Substring(0, 200);
                wasTruncated = true;
            }
            
            if (cleanContent?.Length > 10000)
            {
                cleanContent = cleanContent.Substring(0, 10000);
                wasTruncated = true;
            }

            if (request.IsPasswordProtected)
            {
                 //  GUEST CHECK
                if (user.IsGuest)
                    throw new ValidationException("Guest users cannot password protect notes.");

                if (string.IsNullOrWhiteSpace(user.CommonPasswordHash))
                {
                    if (string.IsNullOrWhiteSpace(request.Password))
                        throw new ValidationException("Password required to lock for the first time");

                    user.CommonPasswordHash =
                        BCrypt.Net.BCrypt.HashPassword(request.Password);
                }
            }

            var note = new Note
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = cleanTitle,
                Content = cleanContent,
                BackgroundColor = request.BackgroundColor,
                FilePaths = request.FilePaths,
                ImagePaths = request.ImagePaths,
                IsPasswordProtected = request.IsPasswordProtected,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();
            
            // Note: Caller should check if truncation occurred (could add metadata to response)
            return note.Id;
        }

        // ================= GET BY ID =================
        public async Task<NoteResponse> GetByIdAsync(
            Guid noteId,
            Guid userId)
        {
            var note = await _context.Notes
                .AsNoTracking()
                .FirstOrDefaultAsync(n =>
                    n.Id == noteId &&
                    n.UserId == userId &&
                    !n.IsDeleted)
                ?? throw new NotFoundException("Note not found");

            var user = await _context.Users.FindAsync(userId)
                ?? throw new NotFoundException("User not found");

            await CleanupExpiredAccessAsync(user);

            var now = DateTime.UtcNow;

            bool hasAccess =
                user.AccessibleFrom.HasValue &&
                user.AccessibleTill.HasValue &&
                now >= user.AccessibleFrom.Value &&
                now <= user.AccessibleTill.Value;

            // Debug logging
            Console.WriteLine($"[GetByIdAsync] NoteId: {noteId}, IsPasswordProtected: {note.IsPasswordProtected}, HasAccess: {hasAccess}");
            if (note.IsPasswordProtected)
            {
                Console.WriteLine($"[GetByIdAsync] AccessibleFrom: {user.AccessibleFrom}, AccessibleTill: {user.AccessibleTill}, Now: {now}");
            }

            // 🔔 Automatically mark notifications as read and reminders as completed when note is opened
            var unreadNotifications = await _context.Notifications
                .Where(notif => notif.NoteId == note.Id && !notif.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notif in unreadNotifications)
                {
                    notif.IsRead = true;
                }

                var reminder = await _context.Reminders
                    .FirstOrDefaultAsync(r => r.NoteId == note.Id && !r.IsCompleted);
                
                if (reminder != null)
                {
                    reminder.IsCompleted = true;
                }

                await _context.SaveChangesAsync();
            }

            return new NoteResponse
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Content, // ALWAYS return content
                FilePaths = note.FilePaths,
                ImagePaths = note.ImagePaths,
                IsPasswordProtected = note.IsPasswordProtected,
                IsLockedByTime = note.IsPasswordProtected && !hasAccess, // Flag for lock status
                BackgroundColor = note.BackgroundColor,
                IsReminderSet = await _context.Reminders.AnyAsync(r => r.NoteId == note.Id && !r.IsCompleted && !r.IsCancelled)
            };
        }

        // ================= LIST =================
        public async Task<PagedResponse<NoteListItemResponse>> GetListAsync(
            Guid userId,
            string? search,
            int pageNumber,
            int pageSize)
        {
            // Edge case: pageNumber should be at least 1
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
                await CleanupExpiredAccessAsync(user);

            var query = _context.Notes
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsDeleted);

            // FILTER (DB LEVEL)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(n =>
                    n.Title != null && n.Title.ToLower().Contains(search));
            }

            //  TOTAL COUNT 
            var totalCount = await query.CountAsync();

            // PAGINATION (DB LEVEL)
            var items = await query
                .OrderByDescending(n => n.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NoteListItemResponse
                {
                    Id = n.Id,
                    Title = n.Title,
                    // Security: Do not send content in list if protected
                    Content = n.IsPasswordProtected ? null : n.Content,
                    FilePaths = n.FilePaths,
                    ImagePaths = n.ImagePaths,
                    IsPasswordProtected = n.IsPasswordProtected,
                    // Reminder is "set" if an active (future or fired-but-unread) reminder exists.
                    // When user reads notification, IsCompleted becomes true and this becomes false.
                    IsReminderSet = _context.Reminders.Any(r => r.NoteId == n.Id && !r.IsCompleted && !r.IsCancelled),
                    UpdatedAt = n.UpdatedAt,
                    BackgroundColor = n.BackgroundColor
                })
                .ToListAsync();

            return new PagedResponse<NoteListItemResponse>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                HasMore = (pageNumber * pageSize) < totalCount
            };
        }

        // ================= UNLOCK =================
        public async Task UnlockProtectedNotesAsync(
            string password,
            int unlockMinutes,
            Guid userId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new NotFoundException("User not found");

            if (string.IsNullOrEmpty(user.CommonPasswordHash))
                throw new ValidationException("No password has been set up for protected notes");

            if (!BCrypt.Net.BCrypt.Verify(password, user.CommonPasswordHash))
                throw new UnauthorizedAccessException("Incorrect password");

            if (unlockMinutes < 1) unlockMinutes = 1;
            if (unlockMinutes > 60) unlockMinutes = 60; // Max 1 hour for security

            var now = DateTime.UtcNow;

            user.AccessibleFrom = now;
            user.AccessibleTill = now.AddMinutes(unlockMinutes);
            user.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(
            Guid noteId,
            UpdateNoteRequest request,
            Guid userId)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n =>
                    n.Id == noteId &&
                    n.UserId == userId &&
                    !n.IsDeleted)
                ?? throw new NotFoundException("Note not found");

            var user = await _context.Users.FindAsync(userId)
                ?? throw new NotFoundException("User not found");

            await CleanupExpiredAccessAsync(user);

            var now = DateTime.UtcNow;

            bool hasAccess =
                user.AccessibleFrom.HasValue &&
                user.AccessibleTill.HasValue &&
                now >= user.AccessibleFrom.Value &&
                now <= user.AccessibleTill.Value;

            // 🔐 If protected and locked, deny update
            if (note.IsPasswordProtected && !hasAccess)
                throw new UnauthorizedAccessException("Protected note is locked. Unlock to edit.");

            // 🧹 SANITIZE
            var cleanTitle = request.Title?.Trim();
            if (cleanTitle?.Length > 200) cleanTitle = cleanTitle.Substring(0, 200);
            
            var cleanContent = request.Content?.Trim();
            if (cleanContent?.Length > 10000) cleanContent = cleanContent.Substring(0, 10000);

            // 🔒 Handle Lock State Changes
            if (request.IsPasswordProtected.HasValue && request.IsPasswordProtected != note.IsPasswordProtected)
            {
                // 🚫 GUEST CHECK
                if (request.IsPasswordProtected.Value && user.IsGuest)
                    throw new ValidationException("Guest users cannot password protect notes.");

                if (request.IsPasswordProtected.Value && string.IsNullOrEmpty(user.CommonPasswordHash))
                {
                   if (string.IsNullOrEmpty(request.Password))
                       throw new ValidationException("Password required to lock for the first time");
                   
                   user.CommonPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }
                
                note.IsPasswordProtected = request.IsPasswordProtected.Value;
            }

            note.Title = cleanTitle;
            note.Content = cleanContent;
            note.FilePaths = request.FilePaths;
            note.ImagePaths = request.ImagePaths;
            note.BackgroundColor = request.BackgroundColor;
            note.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(
            Guid noteId,
            string? password,
            Guid userId)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n =>
                    n.Id == noteId &&
                    n.UserId == userId &&
                    !n.IsDeleted)
                ?? throw new NotFoundException("Note not found");

            var user = await _context.Users.FindAsync(userId)
                ?? throw new NotFoundException("User not found");

            await CleanupExpiredAccessAsync(user);

            var now = DateTime.UtcNow;

            bool hasAccess =
                user.AccessibleFrom.HasValue &&
                user.AccessibleTill.HasValue &&
                now >= user.AccessibleFrom.Value &&
                now <= user.AccessibleTill.Value;

            if (note.IsPasswordProtected && !hasAccess)
                throw new UnauthorizedAccessException("Protected note is locked. Unlock to delete.");

            note.IsDeleted = true;
            note.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }

        public async Task LockProtectedNotesAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new NotFoundException("User not found");

            user.AccessibleFrom = null;
            user.AccessibleTill = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasProtectedNotesAccessAsync(Guid noteId, Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            await CleanupExpiredAccessAsync(user);

            var now = DateTime.UtcNow;
            return user.AccessibleFrom.HasValue &&
                   user.AccessibleTill.HasValue &&
                   now >= user.AccessibleFrom.Value &&
                   now <= user.AccessibleTill.Value;
        }

        public async Task GenerateSummaryAsync(Guid noteId)
        {
            var note = await _context.Notes.FindAsync(noteId)
                ?? throw new NotFoundException("Note not found");

            if (string.IsNullOrWhiteSpace(note.Content))
                throw new ValidationException("Cannot generate summary for empty note");

            // ✅ Only regenerate if note has been updated since last summary generation
            if (note.SummaryUpdatedAt.HasValue && note.UpdatedAt <= note.SummaryUpdatedAt.Value)
            {
                // Note hasn't been modified since last summary - no need to regenerate
                return;
            }

            // 🔥 Clear old summary to allow regeneration polling to work
            note.Summary = null;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _aiPublisher.InitializeAsync();
            await _aiPublisher.PublishAsync(note.Id, note.Content);
        }

        public async Task<Note?> GetNoteWithSummaryAsync(Guid noteId)
        {
            return await _context.Notes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == noteId && !n.IsDeleted);
        }

        // ================= VALIDATION HELPERS =================
        private static bool IsValidS3Path(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            
            // Must start with http:// or https://
            if (!path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return false;

            // Basic URL validation
            return Uri.TryCreate(path, UriKind.Absolute, out var uri) && 
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static bool ContainsHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return input.Contains("<") || input.Contains(">") || 
                   input.Contains("script", StringComparison.OrdinalIgnoreCase) ||
                   input.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
        }
    }
}
