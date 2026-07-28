using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations;

public class DoctorSchedulingPreferenceService(ApplicationDbContext _db) : IDoctorSchedulingPreferenceService
{
    private static readonly TimeSpan DefaultMaxShortfallTolerance = TimeSpan.FromMinutes(15);
    private const int DefaultMaxDaysOutForExactMatch = 7;
    private const bool DefaultAllowShorterSlots = true;
    public async Task<Result<DoctorSchedulingPreferenceDto>> GetForDoctorAsync(Guid doctorId, CancellationToken ct = default)
    {
        var preference = await _db.Set<DoctorSchedulingPreference>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.DoctorId == doctorId, ct);

        preference ??= new DoctorSchedulingPreference { 
            DoctorId = doctorId, 
            MaxShortfallTolerance = DefaultMaxShortfallTolerance,
            MaxDaysOutForExactMatch = DefaultMaxDaysOutForExactMatch, 
            AllowShorterSlots = DefaultAllowShorterSlots 
        };

        return Result.Success(ToDto(preference));
    }

    public async Task<Result<DoctorSchedulingPreferenceDto>> UpdateForDoctorAsync(
        Guid doctorId,
        UpdateDoctorSchedulingPreferenceRequest request,
        CancellationToken ct = default)
    {
        var preference = await _db.Set<DoctorSchedulingPreference>()
            .FirstOrDefaultAsync(p => p.DoctorId == doctorId, ct);

        if (preference is null)
        {
            preference = new DoctorSchedulingPreference { Id = Guid.CreateVersion7(), DoctorId = doctorId };
            _db.Add(preference);
        }

        preference.MaxShortfallTolerance = TimeSpan.FromMinutes(request.MaxShortfallToleranceMinutes);
        preference.MaxDaysOutForExactMatch = request.MaxDaysOutForExactMatch;
        preference.AllowShorterSlots = request.AllowShorterSlots;

        await _db.SaveChangesAsync(ct);

        return Result.Success(ToDto(preference));
    }

    private static DoctorSchedulingPreferenceDto ToDto(DoctorSchedulingPreference p) => new(
        p.Id,
        p.DoctorId,
        (int)p.MaxShortfallTolerance.TotalMinutes,
        p.MaxDaysOutForExactMatch,
        p.AllowShorterSlots);
}
