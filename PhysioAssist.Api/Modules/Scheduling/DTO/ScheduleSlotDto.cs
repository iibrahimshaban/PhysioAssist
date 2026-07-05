namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class ScheduleSlotDto
    {
        public Guid Id { get; init; }
        public Guid DoctorId { get; init; }
        public Guid PatientId { get; init; }
        public DateTime SlotStart { get; init; }
        public DateTime SlotEnd { get; init; }
        public string Status { get; init; } = default!;
    }
}
