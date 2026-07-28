namespace PhysioAssist.Api.Modules.Scheduling.DTO.Guest
{
    public class CreateGuestRequest
    {
        public string FullName { get; init; } = default!;
        public string PhoneNumber { get; init; } = default!;
    }
}
