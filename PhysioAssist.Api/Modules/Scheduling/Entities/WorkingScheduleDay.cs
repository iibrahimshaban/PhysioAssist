namespace PhysioAssist.Api.Modules.Scheduling.Entities
{
    public class WorkingScheduleDay
    {
        public Guid Id { get; set; }
        public Guid WorkingScheduleId { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public WorkingSchedule WorkingSchedule { get; set; } = null!;
    }
}
