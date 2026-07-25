namespace PhysioAssist.Api.Modules.InitialReportModule.DTOs;

public record BookTreatmentSlotRequest(DateTimeOffset SlotStart, DateTimeOffset SlotEnd);
