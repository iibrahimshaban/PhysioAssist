using Microsoft.SemanticKernel;
using PhysioAssist.Api.Modules.Scheduling.DTO.AgentDtos;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;
using System.ComponentModel;
using System.Text;

namespace PhysioAssist.Api.Modules.Scheduling.Plugins;

public class DoctorSchedulePlugin(IDoctorScheduleRecommendationService recommendationService)
{
    private readonly IDoctorScheduleRecommendationService _recommendationService = recommendationService;

    // Caps how many candidates get fed to the model — the service can return many
    // more; the agent only needs enough to reason about and present a shortlist.
    private const int MaxCandidatesReturned = 10;

    [KernelFunction, Description("Finds recommended appointment slots for a doctor for a given session duration. " +
        "Returns a ranked list of candidate slots — exact fits first, then near-miss shorter slots the doctor " +
        "may still choose to accept. Use this whenever you need to suggest, book, or reschedule an appointment.")]
    public async Task<string> GetAvailableSlots(
        [Description("The doctor's ID")] Guid doctorId,
        [Description("Requested session duration in minutes, e.g. 60")] int requestedDurationMinutes,
        [Description("Optional range start (with offset). Omit along with rangeEnd to default to the current week.")] DateTimeOffset? rangeStart = null,
        [Description("Optional range end (with offset). Omit along with rangeStart to default to the current week.")] DateTimeOffset? rangeEnd = null)
    {
        var result = await _recommendationService.GetRecommendedSlotsAsync(
            doctorId,
            TimeSpan.FromMinutes(requestedDurationMinutes),
            rangeStart,
            rangeEnd);

        if (result.IsFailure)
            return $"Could not retrieve availability: {result.Error.Description}";

        if (result.Value.Count == 0)
            return "No available slots found in the given range — consider widening the date range or checking the doctor's active working schedule.";

        var sb = new StringBuilder();
        foreach (var candidate in result.Value.Take(MaxCandidatesReturned))
        {
            sb.Append($"- {candidate.Start:yyyy-MM-dd HH:mm} to {candidate.End:HH:mm}");
            sb.Append(candidate.FitType switch
            {
                SlotFitType.Exact => " (fits the requested duration exactly, whole slot booked",
                SlotFitType.LongerThanRequested => $" (books the requested duration; {candidate.Gap.TotalMinutes:N0} min stays free afterward",
                _ => $" (shorter than requested by {candidate.Gap.TotalMinutes:N0} min — books the entire available slot"
            });
            sb.Append(candidate.IsBeyondPreferredHorizon
                ? ", beyond the doctor's preferred booking horizon)"
                : ")");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
