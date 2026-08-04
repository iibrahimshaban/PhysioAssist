namespace PhysioAssist.Api.Modules.DocumentationModule.Contracts;

public class UpdateProgressNoteNarrativeRequestValidator : AbstractValidator<UpdateProgressNoteNarrativeRequest>
{
    public UpdateProgressNoteNarrativeRequestValidator()
    {
        RuleFor(x => x.Subjective).MaximumLength(4000);
        RuleFor(x => x.Assessment).MaximumLength(4000);
        RuleFor(x => x.Plan).MaximumLength(4000);
    }
}
