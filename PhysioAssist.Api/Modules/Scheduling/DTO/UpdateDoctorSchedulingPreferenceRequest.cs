namespace PhysioAssist.Api.Modules.Scheduling.DTO;

public record UpdateDoctorSchedulingPreferenceRequest(
    int MaxShortfallToleranceMinutes,
    int MaxDaysOutForExactMatch,
    bool AllowShorterSlots
    );

public class UpdateDoctorSchedulingPreferenceRequestValidator : AbstractValidator<UpdateDoctorSchedulingPreferenceRequest>
{
    public UpdateDoctorSchedulingPreferenceRequestValidator()
    {
        RuleFor(x => x.MaxShortfallToleranceMinutes)
            .InclusiveBetween(0, 120)
            .WithMessage("Shortfall tolerance must be between 0 and 120 minutes.");

        RuleFor(x => x.MaxDaysOutForExactMatch)
            .InclusiveBetween(0, 90)
            .WithMessage("Days-out threshold must be between 0 and 90.");
    }
}
