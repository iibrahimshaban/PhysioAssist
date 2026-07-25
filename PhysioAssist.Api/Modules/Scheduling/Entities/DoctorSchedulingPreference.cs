namespace PhysioAssist.Api.Modules.Scheduling.Entities;

public class DoctorSchedulingPreference
{
    public Guid Id { get; set; }

    public Guid DoctorId { get; set; }

    /// <summary>
    /// How much shorter than the requested session duration a slot is allowed to be
    /// and still be offered as a candidate (e.g. 15 min tolerance means a 45-min free
    /// slot can be offered for a 60-min request).
    /// </summary>
    public TimeSpan MaxShortfallTolerance { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Beyond this many days out, stop waiting for an exact-duration match and start
    /// surfacing near-miss (shorter) slots instead, flagged as NextAvailableIsFarOut.
    /// </summary>
    public int MaxDaysOutForExactMatch { get; set; } = 7;

    /// <summary>
    /// If false, the plugin never offers shorter-than-requested slots regardless of
    /// MaxShortfallTolerance — doctor requires exact-duration matches only.
    /// </summary>
    public bool AllowShorterSlots { get; set; } = true;
}
