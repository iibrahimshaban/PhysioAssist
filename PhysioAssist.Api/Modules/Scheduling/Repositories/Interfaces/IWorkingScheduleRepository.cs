using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces
{
    public interface IWorkingScheduleRepository : IBaseRepository<WorkingSchedule>
    {
        Task<WorkingScheduleDay?> GetEffectiveWorkingDayAsync(Guid doctorId, Guid clinicId, 
            DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);
        Task<bool> HasActiveOverrideAsync(Guid doctorId, CancellationToken cancellationToken = default);
        Task<WorkingSchedule?> GetActiveOverrideWithDaysAsync(Guid doctorId, CancellationToken cancellationToken = default);

        // NEW — clinic default
        Task<bool> HasActiveClinicDefaultAsync(Guid clinicId, CancellationToken cancellationToken = default);
        Task<WorkingSchedule?> GetActiveClinicDefaultWithDaysAsync(Guid clinicId, CancellationToken cancellationToken = default);

        // NEW — the two-tier resolution every consumer actually needs
        Task<WorkingSchedule?> GetEffectiveScheduleWithDaysAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);

        Task<WorkingSchedule?> GetByIdWithDaysAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetDoctorIdsWithActiveOverrideAsync( IReadOnlyList<Guid> doctorIds,
            CancellationToken cancellationToken = default);
    }
}
