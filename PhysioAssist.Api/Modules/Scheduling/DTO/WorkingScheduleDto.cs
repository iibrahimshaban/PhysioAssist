namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class WorkingScheduleDto
    {
        public Guid Id { get; init; }
        public Guid DoctorId { get; init; }
        public bool IsActive { get; init; }
        public List<WorkingScheduleDayDto> Days { get; init; } = new();
    }
}
