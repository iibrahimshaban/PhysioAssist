using Microsoft.AspNetCore.Mvc;

namespace PhysioAssist.Api.Shared.ResultPattern;

public static class ResultExtensions
{
    public static ObjectResult ToProblem(this Result result)
    {

        var problemDetails = new ProblemDetails
        {
            Status = result.Error.StatusCode,
            Title = result.Error.Code,          // "User.DisabledUser"
            Detail = result.Error.Description,  // "Disabled user, Please contact the support team."
        };

        return new ObjectResult(problemDetails) { StatusCode = result.Error.StatusCode };

    }
}
