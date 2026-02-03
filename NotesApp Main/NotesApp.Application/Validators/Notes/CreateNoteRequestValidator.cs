using FluentValidation;
using NotesApp.Application.DTOs.Notes;

namespace NotesApp.Application.Validators.Notes
{
    public class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequest>
    {
        public CreateNoteRequestValidator()
        {
            RuleFor(x => x.Title)
                .MaximumLength(200);

            RuleFor(x => x)
                .Must(HaveContent)
                .WithMessage("Note must have at least a title, content, file, or image");

            RuleFor(x => x)
                .Must(HaveValidTimeRange)
                .WithMessage("AccessibleFrom must be earlier than AccessibleTill");
        }

        private bool HaveContent(CreateNoteRequest request)
        {
            return !string.IsNullOrWhiteSpace(request.Title)
                || !string.IsNullOrWhiteSpace(request.Content)
                || (request.FilePaths != null && request.FilePaths.Any())
                || (request.ImagePaths != null && request.ImagePaths.Any());
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
