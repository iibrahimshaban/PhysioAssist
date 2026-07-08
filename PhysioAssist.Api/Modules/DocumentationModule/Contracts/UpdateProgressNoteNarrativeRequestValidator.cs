namespace PhysioAssist.Api.Modules.DocumentationModule.Contracts;

public class UpdateProgressNoteNarrativeRequestValidator : AbstractValidator<UpdateProgressNoteNarrativeRequest>
{
    public UpdateProgressNoteNarrativeRequestValidator()
    {
        RuleFor(x => x.Subjective).NotEmpty();
        RuleFor(x => x.Assessment).NotEmpty();
        RuleFor(x => x.Plan).NotEmpty();
    }
}
