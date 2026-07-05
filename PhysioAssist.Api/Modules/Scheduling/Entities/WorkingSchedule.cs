namespace PhysioAssist.Api.Modules.Scheduling.Entities
{
    /// <summary>
    /// Defines a doctor's recurring weekly availability.
    /// Owns nothing about appointments — purely a template
    /// that the Slot Generator reads from.
    /// One active WorkingSchedule per doctor in V1 (no history/versioning).
    /// </summary>
    public class WorkingSchedule
    {

        public Guid Id { get; set; }
        public Guid DoctorId { get; set; }
        public bool IsActive { get; set; }
        // SlotDurationMinutes removed — duration no longer belongs to the schedule

        public ICollection<WorkingScheduleDay> Days { get; set; } = new List<WorkingScheduleDay>();

    }
}

