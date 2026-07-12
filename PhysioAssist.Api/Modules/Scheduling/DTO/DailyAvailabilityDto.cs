namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class DailyAvailabilityDto
    {
        public DateOnly Date { get; init; }

        public IReadOnlyList<AvailableIntervalDto> Intervals { get; init; }
            = [];
    }
}
