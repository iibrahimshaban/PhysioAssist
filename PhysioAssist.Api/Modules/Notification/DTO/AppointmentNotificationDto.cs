namespace PhysioAssist.Api.Modules.Notification.DTO
{
    public class AppointmentNotificationDto
    {
        public required string PatientEmail { get; init; }
        public required string PatientName { get; init; }
        public required string DoctorName { get; init; }
        public required DateTimeOffset SlotStart { get; init; }
        public required DateTimeOffset SlotEnd { get; init; }
    }
}
