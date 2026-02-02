using FluentValidation;
using NotesApp.Application.DTOs.Notes;

namespace NotesApp.Application.Validators.Notes
{
    public class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequest>
    {
        public CreateNoteRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x)
                .Must(HaveContent)
                .WithMessage("Note must have content, file, or image");

            RuleFor(x => x)
                .Must(HaveValidTimeRange)
                .WithMessage("AccessibleFrom must be earlier than AccessibleTill");
        }

        private bool HaveContent(CreateNoteRequest request)
        {
            return !string.IsNullOrWhiteSpace(request.Content)
                || !string.IsNullOrWhiteSpace(request.FilePath)
                || !string.IsNullOrWhiteSpace(request.ImagePath);
        }

        private bool HaveValidTimeRange(CreateNoteRequest request)
        {
            if (request.AccessibleFrom.HasValue && request.AccessibleTill.HasValue)
            {
                return request.AccessibleFrom <= request.AccessibleTill;
            }
            return true;
        }
    }
}
