namespace PhysioAssist.Api.Modules.Notification.DTO
{
    /// <summary>
    /// Everything a notification template needs to render an appointment-related
    /// email. Populated by the CALLER (AppointmentService) before invoking
    /// INotificationService — the Notification module has no database access
    /// of its own and never resolves patient/doctor identity itself.
    /// </summary>
    public class AppointmentNotificationDto
    {
        public required string PatientEmail { get; init; }
        public required string PatientName { get; init; }
        public required string DoctorName { get; init; }
        public required DateTimeOffset SlotStart { get; init; }
        public required DateTimeOffset SlotEnd { get; init; }
    }
}
