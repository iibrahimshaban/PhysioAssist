namespace PhysioAssist.Api.Modules.Auth.Contracts.Receptionist;

public record UpdateReceptionistRequest(
    string FirstName,
    string LastName,
    string Phone,
    ReceptionistShiftType Shift,
    TimeOnly? From,
    TimeOnly? To,
    List<string> Permissions,
    string? NewPassword
    );
