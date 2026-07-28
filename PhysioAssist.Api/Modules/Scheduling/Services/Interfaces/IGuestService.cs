using PhysioAssist.Api.Modules.Scheduling.DTO.Guest;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Interfaces
{
    public interface IGuestService
    {
        Task<Result<GuestDto>> CreateAsync(CreateGuestRequest request, CancellationToken cancellationToken = default);
        Task<Result<GuestDto>> GetByIdAsync(Guid guestId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GuestDto>> GetByIdsAsync(IEnumerable<Guid> guestIds, CancellationToken cancellationToken = default);
    }
}
