using PhysioAssist.Api.Modules.PatientModule.Services;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations
{

    /// <summary>
    /// Resolves patient/doctor display info needed for appointment notifications
    /// via cross-module service contracts. This is the ONLY place in Scheduling
    /// that reaches into Patient/Doctor — AppointmentService itself never calls
    /// these services directly.
    /// </summary>
    public class AppointmentContactResolver(
      IPatientQueryService patientService,
      IAuthQueryService doctorService)
      : IAppointmentContactResolver
    {
        private readonly IPatientQueryService _patientService = patientService;
        private readonly IAuthQueryService _doctorService = doctorService;

        public async Task<(string Name, string Email)> GetPatientContactAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            var result = await _patientService.GetPatientAsync(patientId, cancellationToken);

            if (result.IsFailure)
            {
                // A ScheduleSlot always stores a real PatientId set at booking time,
                // so a missing patient here means data has gone inconsistent between
                // modules — worth failing loudly rather than silently sending to a
                // blank address. The caller (AppointmentService.BuildNotificationDtoAndNotifyAsync)
                // already wraps this whole resolution step in a try/catch, so throwing
                // here is safe — it gets logged and swallowed at that boundary, never
                // affecting the appointment operation's own success.
                throw new InvalidOperationException($"Patient {patientId} not found while building appointment notification.");
            }

            return (result.Value.FullName, result.Value.EmailAddress);
        }

        public async Task<string> GetDoctorNameAsync(Guid doctorId, CancellationToken cancellationToken = default)
        {
            var result = await _doctorService.GetDoctorById(doctorId, cancellationToken);

            if (result.IsFailure)
                throw new InvalidOperationException($"Doctor {doctorId} not found while building appointment notification.");

            // Per your decision: Title holds the doctor's display name
            // (e.g. "Dr. Sarah Adel"), NOT ClinicName.
            return result.Value.Title ?? "your doctor";
        }
    }
}
