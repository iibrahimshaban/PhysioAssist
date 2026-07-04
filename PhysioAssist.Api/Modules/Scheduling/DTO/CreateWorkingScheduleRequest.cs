namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class CreateWorkingScheduleRequest
    {
        public Guid DoctorId { get; init; }
        public List<WorkingScheduleDayRequest> Days { get; init; } = new();
    }
}
