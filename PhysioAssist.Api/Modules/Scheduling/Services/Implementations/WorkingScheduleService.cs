using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;
using PhysioAssist.Api.Modules.Scheduling.Errors;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;




namespace PhysioAssist.Api.Modules.Scheduling.Services.Implementations
{
    public class WorkingScheduleService(IUnitOfWork unitOfWork, IAppointmentService appointmentService) : IWorkingScheduleService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IAppointmentService _appointmentService = appointmentService;
        

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

        /// <summary>
        /// Replaces the entire set of working days for an existing working schedule.
        /// </summary>
        /// <remarks>
        /// REVISED: previously, already-booked appointments were left untouched even if
        /// they fell outside the new hours. That decision is now reversed — any future
        /// <c>Booked</c> appointment that no longer fits inside the NEW day windows
        /// (its weekday was removed, or its time now falls outside the new start/end)
        /// is automatically cancelled as part of this update. Only applies if this
        /// schedule is currently the doctor's active one; editing a historical/inactive
        /// schedule's days has no effect on live appointments.
        /// </remarks>
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
                await CancelAppointmentsOutsideWindowsAsync(schedule.DoctorId, request.Days, cancellationToken);
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
                await CancelAppointmentsAsync(schedule.DoctorId, windows: null, cancellationToken);
            }

            await _unitOfWork.SaveAsync(cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Permanently deletes a working schedule. If this was the doctor's ACTIVE
        /// schedule, every future Booked appointment is also cancelled, same as
        /// <see cref="DeactivateAsync"/>. Deleting a historical/inactive schedule never
        /// touches appointments — they're already either cancelled from an earlier
        /// deactivation, or governed by a separate, newer active schedule.
        /// </summary>
        public async Task<Result> DeleteAsync(Guid workingScheduleId, CancellationToken cancellationToken = default)
        {
            var schedule = await _unitOfWork.WorkingSchedules.GetByIdWithDaysAsync(workingScheduleId, cancellationToken);

            if (schedule is null)
                return Result.Failure(WorkingScheduleErrors.NotFound(workingScheduleId));

            if (schedule.IsActive)
            {
                //set all appointment cansle 
                await CancelAppointmentsAsync(schedule.DoctorId, windows: null, cancellationToken);
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
            Guid doctorId,
            IReadOnlyCollection<WorkingScheduleDayRequest>? windows,
            CancellationToken cancellationToken)
        {
            
            var BookedAppointment = await _unitOfWork.ScheduleSlots.GetBookedAppointmentsAsync(doctorId, cancellationToken);

            if (BookedAppointment.Count == 0)
                return;

            foreach (var appointment in BookedAppointment)
            {
                
                await _appointmentService.CancelAsync(appointment.Id, cancellationToken);
            }
        }


        private async Task CancelAppointmentsOutsideWindowsAsync(
           Guid doctorId,
           IReadOnlyCollection<WorkingScheduleDayRequest>? windows,
           CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var futureBooked = await _unitOfWork.ScheduleSlots.GetFutureBookedAppointmentsAsync(doctorId, now, cancellationToken);

            if (futureBooked.Count == 0)
                return;

            var windowsByDay = windows?.ToDictionary(w => w.Day);

            foreach (var appointment in futureBooked)
            {
                bool stillFits;

                if (windowsByDay is null)
                {
                    stillFits = false; // no schedule at all anymore
                }
                else if (!windowsByDay.TryGetValue(appointment.SlotStart.DayOfWeek, out var window))
                {
                    stillFits = false; // this weekday is no longer a working day
                }
                else
                {
                    // .TimeOfDay on DateTimeOffset reflects the clock time as stored
                    // with its own offset — the same "wall clock" time the appointment
                    // was booked at, consistent with how WorkingScheduleDay.StartTime/
                    // EndTime represent clock times, not UTC instants.
                    var startTime = TimeOnly.FromTimeSpan(appointment.SlotStart.TimeOfDay);
                    var endTime = TimeOnly.FromTimeSpan(appointment.SlotEnd.TimeOfDay);
                    stillFits = startTime >= window.StartTime && endTime <= window.EndTime;
                }

                if (!stillFits)
                {
                    await   _appointmentService.CancelAsync(appointment.Id, cancellationToken);
                }
            }
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