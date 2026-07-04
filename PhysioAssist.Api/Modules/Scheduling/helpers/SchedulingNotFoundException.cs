namespace PhysioAssist.Api.Modules.Scheduling.helpers
{
    public class SchedulingNotFoundException : Exception
    {
        public SchedulingNotFoundException(string message) : base(message) { }
    }
}
