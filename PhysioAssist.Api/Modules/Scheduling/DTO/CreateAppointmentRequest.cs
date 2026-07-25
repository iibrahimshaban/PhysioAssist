namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class CreateAppointmentRequest
    {
        public Guid DoctorId { get; init; }
        public Guid PatientId { get; init; }
        public DateTimeOffset SlotStart { get; init; }
        public DateTimeOffset SlotEnd { get; init; }

        public Guid? PackageId { get; set; }
    }
}
