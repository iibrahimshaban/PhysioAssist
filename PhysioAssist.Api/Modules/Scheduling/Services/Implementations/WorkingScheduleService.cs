using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.ResultPattern;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations
{
    public class WorkingScheduleService(IUnitOfWork unitOfWork) : IWorkingScheduleService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<WorkingScheduleDto>> CreateAsync(CreateWorkingScheduleRequest request, CancellationToken cancellationToken = default)
        {
            var validation = ValidateDays(request.Days);
            if (validation.IsFailure)
                return Result.Failure<WorkingScheduleDto>(validation.Error);

            var hasActive = await _unitOfWork.WorkingSchedules.HasActiveScheduleAsync(request.DoctorId, cancellationToken);

            if (hasActive)
                return Result.Failure<WorkingScheduleDto>(WorkingScheduleErrors.ActiveScheduleAlreadyExists);

            var schedule = new WorkingSchedule
            {
                Id = Guid.CreateVersion7(),
                DoctorId = request.DoctorId,
                IsActive = true,
                Days = request.Days.Select(d => new WorkingScheduleDay
                {
                    Id = Guid.CreateVersion7(),
                    Day = d.Day,
                    StartTime = d.StartTime,
                    EndTime = d.EndTime
                }).ToList()
            };

            await _unitOfWork.WorkingSchedules.AddAsync(schedule);
            await _unitOfWork.SaveAsync(cancellationToken);

            return Result.Success(MapToDto(schedule));
        }

        public async Task<Result<WorkingScheduleDto>> GetActiveByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
        {
            var schedule = await _unitOfWork.WorkingSchedules.GetActiveScheduleWithDaysAsync(doctorId, cancellationToken);

            return schedule is null
                ? Result.Failure<WorkingScheduleDto>(WorkingScheduleErrors.NoActiveScheduleFound(doctorId))
                : Result.Success(MapToDto(schedule));
        }

        public async Task<Result<WorkingScheduleDto>> UpdateDaysAsync(Guid workingScheduleId, UpdateWorkingScheduleDaysRequest request, CancellationToken cancellationToken = default)
        {
            var validation = ValidateDays(request.Days);
            if (validation.IsFailure)
                return Result.Failure<WorkingScheduleDto>(validation.Error);

            var schedule = await _unitOfWork.WorkingSchedules.GetByIdWithDaysAsync(workingScheduleId, cancellationToken);

            if (schedule is null)
                return Result.Failure<WorkingScheduleDto>(WorkingScheduleErrors.NotFound(workingScheduleId));

            // Replace the day set entirely — simplest correct approach for V1.
            // NOTE: this does NOT touch already-booked ScheduleSlots. Per our earlier
            // decision, booked appointments survive a schedule edit even if they now
            // fall outside the new hours — that's an accepted historical exception,
            // not something this method is responsible for cleaning up.
            schedule.Days.Clear();

            foreach (var day in request.Days)
            {
                schedule.Days.Add(new WorkingScheduleDay
                {
                    Id = Guid.CreateVersion7(),
                    WorkingScheduleId = schedule.Id,
                    Day = day.Day,
                    StartTime = day.StartTime,
                    EndTime = day.EndTime
                });
            }

            await _unitOfWork.SaveAsync(cancellationToken);

            return Result.Success(MapToDto(schedule));
        }

        public async Task<Result> DeactivateAsync(Guid workingScheduleId, CancellationToken cancellationToken = default)
        {
            var schedule = await _unitOfWork.WorkingSchedules.GetByIdWithDaysAsync(workingScheduleId, cancellationToken);

            if (schedule is null)
                return Result.Failure(WorkingScheduleErrors.NotFound(workingScheduleId));

            schedule.IsActive = false;

            await _unitOfWork.SaveAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid workingScheduleId, CancellationToken cancellationToken = default)
        {
            var schedule = await _unitOfWork.WorkingSchedules.GetByIdWithDaysAsync(workingScheduleId, cancellationToken);

            if (schedule is null)
                return Result.Failure(WorkingScheduleErrors.NotFound(workingScheduleId));

            _unitOfWork.WorkingSchedules.Delete(schedule);
            await _unitOfWork.SaveAsync(cancellationToken);

            return Result.Success();
        }

        private static Result ValidateDays(List<WorkingScheduleDayRequest> days)
        {
            if (days.Count == 0)
                return Result.Failure(WorkingScheduleErrors.NoWorkingDaysProvided);

            foreach (var day in days)
            {
                if (day.EndTime <= day.StartTime)
                    return Result.Failure(WorkingScheduleErrors.InvalidDayTimeRange(day.Day));
            }

            var duplicates = days.GroupBy(d => d.Day).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (duplicates.Count > 0)
                return Result.Failure(WorkingScheduleErrors.DuplicateDays(duplicates));

            return Result.Success();
        }

        private static WorkingScheduleDto MapToDto(WorkingSchedule schedule) => new()
        {
            Id = schedule.Id,
            DoctorId = schedule.DoctorId,
            IsActive = schedule.IsActive,
            Days = schedule.Days.Select(d => new WorkingScheduleDayDto
            {
                Day = d.Day,
                StartTime = d.StartTime,
                EndTime = d.EndTime
            }).ToList()
        };
    }
}