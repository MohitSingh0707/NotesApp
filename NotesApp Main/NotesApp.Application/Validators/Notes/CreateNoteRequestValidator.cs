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
                .Must(HaveBasicInfo)
                .WithMessage("Note must have at least a Title or Content.");

            RuleFor(x => x)
                .Must(HaveValidTimeRange)
                .WithMessage("AccessibleFrom must be earlier than AccessibleTill");
        }

        private bool HaveBasicInfo(CreateNoteRequest request)
        {
            // The user specifically wants Title or Content to be mandatory
            return !string.IsNullOrWhiteSpace(request.Title)
                || !string.IsNullOrWhiteSpace(request.Content);
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
