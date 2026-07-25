namespace PhysioAssist.Api.Shared.Dtos.Schedule
{
    public class ScheduleSlotDto
    {
        public Guid Id { get; init; }
        public Guid DoctorId { get; init; }
        public Guid PatientId { get; init; }
        public DateTimeOffset SlotStart { get; init; }
        public DateTimeOffset SlotEnd { get; init; }
        public string Status { get; init; } = default!;
    }
}
