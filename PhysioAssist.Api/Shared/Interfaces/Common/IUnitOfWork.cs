using Microsoft.EntityFrameworkCore.Storage;
using PhysioAssist.Api.Modules.InitialReportModule.Repositories;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Repositories.Interfaces;

namespace PhysioAssist.Api.Shared.Interfaces.Common;

public interface IUnitOfWork 
{
    IScheduleSlotRepository ScheduleSlots { get; }
    IWorkingScheduleRepository WorkingSchedules { get; }
    IWorkingScheduleDayRepository WorkingScheduleDays { get; }
    ITreatmentSchedulePlanRepository TreatmentSchedulePlans { get; }
    IGuestRepository Guests { get; }

    Task SaveAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
