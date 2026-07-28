using PhysioAssist.Api.Modules.Scheduling.DTO.Guest;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations
{
    public class GuestService(IUnitOfWork unitOfWork) : IGuestService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<GuestDto>> CreateAsync(CreateGuestRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return Result.Failure<GuestDto>(GuestErrors.FullNameRequired);

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Result.Failure<GuestDto>(GuestErrors.PhoneNumberRequired);

            var guest = new Guest
            {
                Id = Guid.CreateVersion7(),
                FullName = request.FullName.Trim(),
                PhoneNumber = request.PhoneNumber.Trim()
            };

            await _unitOfWork.Guests.AddAsync(guest);
            await _unitOfWork.SaveAsync(cancellationToken);

            return Result.Success(MapToDto(guest));
        }

        public async Task<Result<GuestDto>> GetByIdAsync(Guid guestId, CancellationToken cancellationToken = default)
        {
            var guest = await _unitOfWork.Guests.GetByIdAsync(guestId, cancellationToken);

            return guest is null
                ? Result.Failure<GuestDto>(GuestErrors.NotFound(guestId))
                : Result.Success(MapToDto(guest));
        }


        public async Task<IReadOnlyList<GuestDto>> GetByIdsAsync(IEnumerable<Guid> guestIds, CancellationToken cancellationToken = default)
        {
            var guests = await _unitOfWork.Guests.GetByIdsAsync(guestIds, cancellationToken);
            return guests.Select(MapToDto).ToList();
        }

        private static GuestDto MapToDto(Guest guest) => new()
        {
            Id = guest.Id,
            FullName = guest.FullName,
            PhoneNumber = guest.PhoneNumber
        };
    }
}
