namespace PhysioAssist.Api.Modules.Scheduling.helpers
{
    public class SchedulingConflictException: Exception
    {
        public SchedulingConflictException(string message) : base(message) { }
    }
}
