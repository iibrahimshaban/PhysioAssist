using Microsoft.AspNetCore.Mvc;

namespace PhysioAssist.Api.Extensions;

public static class ResultExtensions
{
    public static ObjectResult ToProblem(this Result result)
    {

        var Problem = Results.Problem(statusCode: result.Error.StatusCode);
        var problemDetails = Problem.GetType().GetProperty(nameof(ProblemDetails))!.GetValue(Problem) as ProblemDetails;

        problemDetails!.Extensions = new Dictionary<string, object?>
            {
                 {
                    "Errors" , new []
                    {
                        result.Error.Code,
                        result.Error.Description
                    }
                 }
            };

        return new ObjectResult(problemDetails);
    }
}
