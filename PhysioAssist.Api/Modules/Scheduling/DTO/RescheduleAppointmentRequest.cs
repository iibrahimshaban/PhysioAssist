namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class RescheduleAppointmentRequest
    {
        public DateTimeOffset NewSlotStart { get; init; }
        public DateTimeOffset NewSlotEnd { get; init; }
    }
}
