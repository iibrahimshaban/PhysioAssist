using Mapster;
using PhysioAssist.Api.Modules.Auth.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Dtos.Doctor;

namespace PhysioAssist.Api.Modules.Auth.Services;

public class AuthQueryService(ApplicationDbContext context) : IAuthQueryService
{
    private readonly ApplicationDbContext _context = context;
    public async Task<Result<DoctorResponse>> GetDoctorById(Guid id, CancellationToken cancellationToken = default)
    {
        var doctor = await _context.Doctors.FindAsync([id, cancellationToken], cancellationToken: cancellationToken);
        if (doctor == null)
        {
            return Result.Failure<DoctorResponse>(DoctorErrors.DoctorNotFound);
        }

        var doctorResponse = doctor.Adapt<DoctorResponse>();

        return Result.Success(doctorResponse);
    }
}
