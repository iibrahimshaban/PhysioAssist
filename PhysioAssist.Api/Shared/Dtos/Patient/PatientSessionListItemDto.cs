namespace PhysioAssist.Api.Shared.Dtos.Patient;

public sealed class PatientSessionListItemDto
{
    public Guid SlotId { get; init; }
    public int SessionNumber { get; init; }
    public DateTimeOffset SlotStart { get; init; }
    public DateTimeOffset SlotEnd { get; init; }
    public SlotStatus Status { get; init; }
}
