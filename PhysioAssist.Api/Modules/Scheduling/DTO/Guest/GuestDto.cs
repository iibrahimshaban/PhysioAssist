namespace PhysioAssist.Api.Modules.Scheduling.DTO.Guest
{
    public class GuestDto
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = default!;
        public string PhoneNumber { get; init; } = default!;
    }
}
