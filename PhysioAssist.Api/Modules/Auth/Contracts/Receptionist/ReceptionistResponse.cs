namespace PhysioAssist.Api.Modules.Auth.Contracts.Receptionist;

public record ReceptionistResponse(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    bool IsActive,
    ReceptionistShiftType Shift,
    TimeOnly? From,
    TimeOnly? To,
    Guid ManagingDoctorId,
    List<string> Permissions,
    string? ProfilePictureUrl
    );
