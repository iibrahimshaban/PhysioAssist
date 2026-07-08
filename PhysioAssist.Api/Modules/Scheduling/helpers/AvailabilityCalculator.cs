using PhysioAssist.Api.Modules.Scheduling.DTO;
using PhysioAssist.Api.Modules.Scheduling.Entities;

namespace PhysioAssist.Api.Modules.Scheduling.helpers
{
    public static class AvailabilityCalculator
    {
        public static List<AvailableIntervalDto> CalculateFreeIntervals(
            DateOnly date,
            WorkingScheduleDay workingDay,
            List<ScheduleSlot> existingAppointments)
        {
            // Build the working window in UTC
            var windowStart = new DateTimeOffset(
                DateTime.SpecifyKind(
                    date.ToDateTime(workingDay.StartTime),
                    DateTimeKind.Utc));

            var windowEnd = new DateTimeOffset(
                DateTime.SpecifyKind(
                    date.ToDateTime(workingDay.EndTime),
                    DateTimeKind.Utc));

            var result = new List<AvailableIntervalDto>();
            var cursor = windowStart;

            foreach (var appointment in existingAppointments.OrderBy(a => a.SlotStart))
            {
                if (appointment.SlotStart > cursor)
                {
                    result.Add(new AvailableIntervalDto
                    {
                        Start = cursor,
                        End = appointment.SlotStart
                    });
                }

                if (appointment.SlotEnd > cursor)
                {
                    cursor = appointment.SlotEnd;
                }
            }

            if (cursor < windowEnd)
            {
                result.Add(new AvailableIntervalDto
                {
                    Start = cursor,
                    End = windowEnd
                });
            }

            return result;
        }
    }
}
