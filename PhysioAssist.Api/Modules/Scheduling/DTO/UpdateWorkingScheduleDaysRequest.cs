namespace PhysioAssist.Api.Modules.Scheduling.DTO
{
    public class UpdateWorkingScheduleDaysRequest
    {
        public List<WorkingScheduleDayRequest> Days { get; init; } = new();
    }
}
