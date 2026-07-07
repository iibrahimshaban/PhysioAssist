namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class RescheduleAppointmentRequest
    {
        public DateTime NewSlotStart { get; init; }
        public DateTime NewSlotEnd { get; init; }
    }
}
