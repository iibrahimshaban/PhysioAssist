namespace PhysioAssist.Api.Modules.Auth.Contracts.Authentication;

public record ConfirmEmailRequest(
       string Email,
       string Code
       );
