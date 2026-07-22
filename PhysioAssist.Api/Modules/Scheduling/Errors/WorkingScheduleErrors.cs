namespace PhysioAssist.Api.Modules.Scheduling.Errors
{
    public class WorkingScheduleErrors
    {
        public static Error NoWorkingDaysProvided => new(
            "WorkingSchedule.NoWorkingDaysProvided",
            "At least one working day is required.",
            StatusCodes.Status400BadRequest);

        public static Error InvalidDayTimeRange(DayOfWeek day) => new(
            "WorkingSchedule.InvalidDayTimeRange",
            $"{day}: end time must be after start time.",
            StatusCodes.Status400BadRequest);

        public static Error DuplicateDays(IEnumerable<DayOfWeek> days) => new(
            "WorkingSchedule.DuplicateDays",
            $"Duplicate day(s) in request: {string.Join(", ", days)}",
            StatusCodes.Status400BadRequest);

        public static Error ActiveScheduleAlreadyExists => new(
            "WorkingSchedule.ActiveScheduleAlreadyExists",
            "This doctor already has an active working schedule. Deactivate it before creating a new one.",
            StatusCodes.Status409Conflict);

        public static Error NoActiveScheduleFound(Guid doctorId) => new(
            "WorkingSchedule.NoActiveScheduleFound",
            $"Doctor {doctorId} has no active working schedule.",
            StatusCodes.Status404NotFound);

        public static Error NotFound(Guid workingScheduleId) => new(
            "WorkingSchedule.NotFound",
            $"WorkingSchedule {workingScheduleId} was not found.",
            StatusCodes.Status404NotFound);
    }
}
