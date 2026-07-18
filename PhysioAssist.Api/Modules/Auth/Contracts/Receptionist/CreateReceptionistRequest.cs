namespace PhysioAssist.Api.Modules.Auth.Contracts.Receptionist;

public record CreateReceptionistRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Password,
    ReceptionistShiftType Shift,
    TimeOnly? From,
    TimeOnly? To,
    List<string> Permissions);
