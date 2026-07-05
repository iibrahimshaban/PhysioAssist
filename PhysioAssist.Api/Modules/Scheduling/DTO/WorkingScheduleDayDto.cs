namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class WorkingScheduleDayDto
    {
        public DayOfWeek Day { get; init; }
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
    }
}
