// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using NotesApp.Application.DTOs.Common;
// using NotesApp.Application.DTOs.Notes;
// using NotesApp.Domain.Entities;

// namespace NotesApp.Application.Interfaces.Notes
// {
//     public interface INoteService
//     {
//         // ================= CREATE =================
//         Task<Guid> CreateAsync(CreateNoteRequest request, Guid userId);

//         // ================= READ =================
//         Task<NoteResponse> GetByIdAsync(Guid noteId, Guid userId);
//         Task<PagedResponse<NoteListItemResponse>> GetListAsync(Guid userId, string? search, int pageNumber, int pageSize);

//         // ================= UPDATE =================
//         Task UpdateAsync(Guid noteId, UpdateNoteRequest request, Guid userId);

//         // ================= LOCK =================
//         Task LockNoteAsync(Guid noteId, LockNoteRequest request, Guid userId);

//         // ================= UNLOCK (USER LEVEL) =================
//         // 🔥 Ek baar password → ALL protected notes unlock
//         Task UnlockProtectedNotesAsync(
//             string password,
//             DateTime accessibleTill,
//             Guid userId
//         );

//         // 🔐 Check if user can access protected notes
//         Task<bool> HasProtectedNotesAccessAsync(Guid userId);

//         // ================= DELETE =================
//         Task DeleteAsync(Guid noteId, Guid userId);

//         // ================= SUMMARY =================
//         Task GenerateSummaryAsync(Guid noteId);
//         Task<Note?> GetNoteWithSummaryAsync(Guid noteId);
//     }
// }

using System;
using System.Threading.Tasks;
using NotesApp.Application.DTOs.Common;
using NotesApp.Application.DTOs.Notes;
using NotesApp.Domain.Entities;

namespace NotesApp.Application.Interfaces.Notes
{
    public interface INoteService
    {
        // ================= CREATE =================
        Task<Guid> CreateAsync(
            CreateNoteRequest request,
            Guid userId
        );

        // ================= READ =================
        Task<NoteResponse> GetByIdAsync(
            Guid noteId,
            Guid userId,
            string? password
        );

        Task<PagedResponse<NoteListItemResponse>> GetListAsync(
            Guid userId,
            string? search,
            int pageNumber,
            int pageSize
        );

        // ================= UPDATE =================
        Task UpdateAsync(
            Guid noteId,
            UpdateNoteRequest request,
            Guid userId
        );

        // ================= DELETE =================
        Task DeleteAsync(
            Guid noteId,
            string? password,
            Guid userId
        );

        // ================= LOCK =================
        Task LockNoteAsync(
            Guid noteId,
            LockNoteRequest request,
            Guid userId
        );

        // ================= UNLOCK (NOTE LEVEL) =================
        Task UnlockProtectedNotesAsync(
    Guid noteId,
    string password,
    int unlockMinutes,
    Guid userId
);


        // ================= ACCESS CHECK (NOTE LEVEL) =================
        Task<bool> HasProtectedNotesAccessAsync(
            Guid noteId,
            Guid userId
        );

        // ================= SUMMARY =================
        Task GenerateSummaryAsync(Guid noteId);
        Task<Note?> GetNoteWithSummaryAsync(Guid noteId);
    }
}
