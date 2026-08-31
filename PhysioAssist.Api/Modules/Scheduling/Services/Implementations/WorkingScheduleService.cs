using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations
{
    public class WorkingScheduleService(
        IUnitOfWork unitOfWork,
        IAppointmentService appointmentService,
        IClinicDoctorResolver _clinicDoctorResolver) : IWorkingScheduleService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IAppointmentService _appointmentService = appointmentService; 

        public async Task<Result<WorkingScheduleDto>> GetEffectiveByDoctorAsync(
            Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
        {
            var schedule = await _unitOfWork.WorkingSchedules.GetEffectiveScheduleWithDaysAsync(doctorId, clinicId, cancellationToken);

            return schedule is null
                ? Result.Failure<WorkingScheduleDto>(WorkingScheduleErrors.NoActiveScheduleFound(doctorId))
                : Result.Success(MapToDto(schedule));
        }
        public async Task<Result<WorkingScheduleDto>> CreateClinicDefaultAsync(
        Guid clinicId, CreateWorkingScheduleRequest request, CancellationToken cancellationToken = default)
        {
            var validation = ValidateDays(request.Days);
            if (validation.IsFailure)
                return Result.Failure<WorkingScheduleDto>(validation.Error);

            var hasActive = await _unitOfWork.WorkingSchedules.HasActiveClinicDefaultAsync(clinicId, cancellationToken);
            if (hasActive)
                return Result.Failure<WorkingScheduleDto>(WorkingScheduleErrors.ActiveScheduleAlreadyExists);

            var schedule = new WorkingSchedule
            {
                Id = Guid.CreateVersion7(),
                ClinicId = clinicId,
                DoctorId = null,
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
        public async Task<Result<WorkingScheduleDto>> CreateDoctorOverrideAsync(
        Guid clinicId, Guid doctorId, CreateWorkingScheduleRequest request, CancellationToken cancellationToken = default)
        {
            var validation = ValidateDays(request.Days);
            if (validation.IsFailure)
                return Result.Failure<WorkingScheduleDto>(validation.Error);

            var hasActive = await _unitOfWork.WorkingSchedules.HasActiveOverrideAsync(doctorId, cancellationToken);
            if (hasActive)
                return Result.Failure<WorkingScheduleDto>(WorkingScheduleErrors.ActiveScheduleAlreadyExists);

            var schedule = new WorkingSchedule
            {
                Id = Guid.CreateVersion7(),
                ClinicId = clinicId,
                DoctorId = doctorId,
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
        public async Task<Result<WorkingScheduleDto>> UpdateDaysAsync(Guid workingScheduleId, UpdateWorkingScheduleDaysRequest request, CancellationToken cancellationToken = default)
        {
            var validation = ValidateDays(request.Days);
            if (validation.IsFailure)
                return Result.Failure<WorkingScheduleDto>(validation.Error);

            var schedule = await _unitOfWork.WorkingSchedules.GetByIdWithDaysAsync(workingScheduleId, cancellationToken);

            if (schedule is null)
                return Result.Failure<WorkingScheduleDto>(WorkingScheduleErrors.NotFound(workingScheduleId));

            if (schedule.IsActive)
            {
                // Cancel future booked appointments that no longer fit the NEW windows.
                // Uses request.Days (the incoming shape) rather than schedule.Days,
                // since schedule.Days hasn't been rebuilt yet at this point.
                await CancelAppointmentsOutsideWindowsAsync(schedule, request.Days, cancellationToken);
            }

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

        /// <summary>
        /// Deactivates a working schedule. If this was the doctor's ACTIVE schedule,
        /// every future Booked appointment is also cancelled — with no active schedule
        /// left, those appointments are no longer anchored to any working hours.
        /// Deactivating an already-inactive schedule is a no-op with no appointment impact.
        /// </summary>
        public async Task<Result> DeactivateAsync(Guid workingScheduleId, CancellationToken cancellationToken = default)
        {
            var schedule = await _unitOfWork.WorkingSchedules.GetByIdWithDaysAsync(workingScheduleId, cancellationToken);

            if (schedule is null)
                return Result.Failure(WorkingScheduleErrors.NotFound(workingScheduleId));

            var wasActive = schedule.IsActive;
            schedule.IsActive = false;

            if (wasActive)
            {
                await CancelAppointmentsAsync(schedule, windows: null, cancellationToken);
            }

            await _unitOfWork.SaveAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid workingScheduleId, CancellationToken cancellationToken = default)
        {
            var schedule = await _unitOfWork.WorkingSchedules.GetByIdWithDaysAsync(workingScheduleId, cancellationToken);

            if (schedule is null)
                return Result.Failure(WorkingScheduleErrors.NotFound(workingScheduleId));

            if (schedule.IsActive)
            {
                //set all appointment cansle 
                await CancelAppointmentsAsync(schedule, windows: null, cancellationToken);
            }

            _unitOfWork.WorkingSchedules.Delete(schedule);
            await _unitOfWork.SaveAsync(cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Cancels every future Booked appointment for a doctor that no longer fits
        /// within the given set of day windows. Pass <c>null</c> for <paramref name="windows"/>
        /// to mean "no schedule at all" — every future booked appointment is cancelled
        /// unconditionally in that case (used by Deactivate/Delete).
        /// "Future" = SlotStart at or after the current instant; past Booked appointments
        /// (which shouldn't normally exist — Complete/Cancel/NoShow should have resolved
        /// them by then) are left alone rather than retroactively cancelled.
        /// </summary>
        private async Task CancelAppointmentsAsync(
        WorkingSchedule schedule,
        IReadOnlyCollection<WorkingScheduleDayRequest>? windows,
        CancellationToken cancellationToken)
        {
            var affectedDoctorIds = schedule.DoctorId.HasValue
                ? new List<Guid> { schedule.DoctorId.Value }
                : await _clinicDoctorResolver.GetDoctorIdsForClinicAsync(schedule.ClinicId, cancellationToken);

            foreach (var doctorId in affectedDoctorIds)
            {
                var booked = await _unitOfWork.ScheduleSlots.GetBookedAppointmentsAsync([doctorId], cancellationToken);
                foreach (var appointment in booked)
                    await _appointmentService.CancelAsync(appointment.Id, cancellationToken);
            }
        }


        private async Task CancelAppointmentsOutsideWindowsAsync(
            WorkingSchedule schedule,
            IReadOnlyCollection<WorkingScheduleDayRequest>? windows,
            CancellationToken cancellationToken)
        {
            var affectedDoctorIds = schedule.DoctorId.HasValue
                ? new List<Guid> { schedule.DoctorId.Value }
                : await GetDoctorsUsingClinicDefaultAsync(schedule.ClinicId, cancellationToken);

            if (affectedDoctorIds.Count == 0)
                return;

            var now = DateTimeOffset.UtcNow;
            // FIX: GetFutureBookedAppointmentsAsync now takes a doctor-id LIST
            var futureBooked = await _unitOfWork.ScheduleSlots.GetFutureBookedAppointmentsAsync(affectedDoctorIds, now, cancellationToken);

            if (futureBooked.Count == 0)
                return;

            var windowsByDay = windows?.ToDictionary(w => w.Day);

            foreach (var appointment in futureBooked)
            {
                bool stillFits;

                if (windowsByDay is null)
                {
                    stillFits = false;
                }
                else if (!windowsByDay.TryGetValue(appointment.SlotStart.DayOfWeek, out var window))
                {
                    stillFits = false;
                }
                else
                {
                    var startTime = TimeOnly.FromTimeSpan(appointment.SlotStart.TimeOfDay);
                    var endTime = TimeOnly.FromTimeSpan(appointment.SlotEnd.TimeOfDay);
                    stillFits = startTime >= window.StartTime && endTime <= window.EndTime;
                }

                if (!stillFits)
                {
                    await _appointmentService.CancelAsync(appointment.Id, cancellationToken);
                }
            }
        }
        private async Task<List<Guid>> GetDoctorsUsingClinicDefaultAsync(Guid clinicId, CancellationToken cancellationToken)
        {
            var clinicDoctorIds = await _clinicDoctorResolver.GetDoctorIdsForClinicAsync(clinicId, cancellationToken);
            if (clinicDoctorIds.Count == 0)
                return [];

            var doctorsWithOverride = await _unitOfWork.WorkingSchedules.GetDoctorIdsWithActiveOverrideAsync(clinicDoctorIds, cancellationToken);

            return clinicDoctorIds.Except(doctorsWithOverride).ToList();
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