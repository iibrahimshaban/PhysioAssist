using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Shared.ResultPattern;

namespace PhysioAssist.Api.Modules.Scheduling.Errors
{
    /// <summary>
    /// Centralized catalog of domain errors that can occur while working with
    /// appointments (ScheduleSlots).
    /// </summary>
    public static class AppointmentErrors
    {
        public static Error EndBeforeStart => new(
            "Appointment.EndBeforeStart",
            "SlotEnd must be after SlotStart.",
            StatusCodes.Status400BadRequest);

        public static Error DurationTooShort(int minMinutes) => new(
            "Appointment.DurationTooShort",
            $"Appointment must be at least {minMinutes} minutes.",
            StatusCodes.Status400BadRequest);

        public static Error DurationTooLong(int maxMinutes) => new(
            "Appointment.DurationTooLong",
            $"Appointment cannot exceed {maxMinutes} minutes.",
            StatusCodes.Status400BadRequest);

        public static Error SpansMultipleDays => new(
            "Appointment.SpansMultipleDays",
            "Appointment cannot span multiple days.",
            StatusCodes.Status400BadRequest);

        public static Error NotFound(Guid appointmentId) => new(
            "Appointment.NotFound",
            $"Appointment {appointmentId} was not found.",
            StatusCodes.Status404NotFound);

        public static Error DoctorNotWorking(DayOfWeek day) => new(
            "Appointment.DoctorNotWorking",
            $"Doctor is not working on {day}.",
            StatusCodes.Status409Conflict);

        public static Error OutsideWorkingHours(TimeOnly start, TimeOnly end) => new(
            "Appointment.OutsideWorkingHours",
            $"Appointment must be within working hours ({start} - {end}).",
            StatusCodes.Status409Conflict);

        public static Error Overlap => new(
            "Appointment.Overlap",
            "This appointment overlaps an existing appointment.",
            StatusCodes.Status409Conflict);

        public static Error InvalidStatusForCancel(SlotStatus status) => new(
            "Appointment.InvalidStatusForCancel",
            $"Cannot cancel an appointment in status {status}.",
            StatusCodes.Status409Conflict);

        public static Error InvalidStatusForComplete(SlotStatus status) => new(
            "Appointment.InvalidStatusForComplete",
            $"Cannot complete an appointment in status {status}.",
            StatusCodes.Status409Conflict);

        public static Error InvalidStatusForNoShow(SlotStatus status) => new(
            "Appointment.InvalidStatusForNoShow",
            $"Cannot mark no-show on an appointment in status {status}.",
            StatusCodes.Status409Conflict);

        public static Error InvalidStatusForReschedule(SlotStatus status) => new(
            "Appointment.InvalidStatusForReschedule",
            $"Cannot reschedule an appointment in status {status}.",
            StatusCodes.Status409Conflict);
    }
}