using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.helpers;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations
{
    public class WorkingScheduleService(IUnitOfWork unitOfWork) : IWorkingScheduleService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<WorkingScheduleDto> CreateAsync(CreateWorkingScheduleRequest request, CancellationToken cancellationToken = default)
        {
            ValidateDays(request.Days);

            var hasActive = await _unitOfWork.WorkingSchedules.HasActiveScheduleAsync(request.DoctorId, cancellationToken);

            if (hasActive)
                throw new SchedulingConflictException("This doctor already has an active working schedule. Deactivate it before creating a new one.");

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

            return MapToDto(schedule);
        }

        public async Task<WorkingScheduleDto?> GetActiveByDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
        {
            var schedule = await _unitOfWork.WorkingSchedules.GetActiveScheduleWithDaysAsync(doctorId, cancellationToken);
            return schedule is null ? null : MapToDto(schedule);
        }

        public async Task<WorkingScheduleDto> UpdateDaysAsync(Guid workingScheduleId, UpdateWorkingScheduleDaysRequest request, CancellationToken cancellationToken = default)
        {
            ValidateDays(request.Days);

            var schedule = await _unitOfWork.WorkingSchedules.GetByIdWithDaysAsync(workingScheduleId, cancellationToken);

            if (schedule is null)
                throw new SchedulingNotFoundException($"WorkingSchedule {workingScheduleId} was not found.");

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

            return MapToDto(schedule);
        }

        public async Task DeactivateAsync(Guid workingScheduleId, CancellationToken cancellationToken = default)
        {
            var schedule = await _unitOfWork.WorkingSchedules.GetByIdWithDaysAsync(workingScheduleId, cancellationToken);

            if (schedule is null)
                throw new SchedulingNotFoundException($"WorkingSchedule {workingScheduleId} was not found.");

            schedule.IsActive = false;

            await _unitOfWork.SaveAsync(cancellationToken);
        }

        private static void ValidateDays(List<WorkingScheduleDayRequest> days)
        {
            if (days.Count == 0)
                throw new ArgumentException("At least one working day is required.");

            foreach (var day in days)
            {
                if (day.EndTime <= day.StartTime)
                    throw new ArgumentException($"{day.Day}: end time must be after start time.");
            }

            var duplicates = days.GroupBy(d => d.Day).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (duplicates.Count > 0)
                throw new ArgumentException($"Duplicate day(s) in request: {string.Join(", ", duplicates)}");
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
