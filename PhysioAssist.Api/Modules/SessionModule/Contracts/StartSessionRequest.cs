namespace PhysioAssist.Api.Modules.SessionModule.Contracts;

public sealed record StartSessionRequest(Guid PatientId, Guid ScheduleSlotId);
