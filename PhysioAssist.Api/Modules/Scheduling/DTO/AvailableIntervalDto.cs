namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class AvailableIntervalDto
    {
        public DateTimeOffset Start { get; init; }
        public DateTimeOffset End { get; init; }
    }
}
