using FluentValidation;
using NotesApp.Application.DTOs.Notes;

namespace NotesApp.Application.Validators.Notes
{
    public class UpdateNoteRequestValidator : AbstractValidator<UpdateNoteRequest>
    {
        public UpdateNoteRequestValidator()
        {
            RuleFor(x => x.Title)
                .MaximumLength(200);

            RuleFor(x => x)
                .Must(HaveBasicInfo)
                .WithMessage("Note must have at least a Title or Content.");
        }

        private bool HaveBasicInfo(UpdateNoteRequest request)
        {
            Console.WriteLine("Title: " + request.Title);
            Console.WriteLine("Content: " + request.Content);
            // At least one of Title or Content must be present for a valid note
            return !string.IsNullOrWhiteSpace(request.Title)
                || !string.IsNullOrWhiteSpace(request.Content);
        }
    }
}
