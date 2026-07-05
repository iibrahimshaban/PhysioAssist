namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class CreateAppointmentRequest
    {
        public Guid DoctorId { get; init; }
        public Guid PatientId { get; init; }
        public DateTime SlotStart { get; init; }
        public DateTime SlotEnd { get; init; }
    }
}
