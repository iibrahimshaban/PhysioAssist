namespace PhysioAssist.Api.Modules.PackageModule.DTOs;

public record BookTreatmentSlotRequest(DateTimeOffset SlotStart, DateTimeOffset SlotEnd);
