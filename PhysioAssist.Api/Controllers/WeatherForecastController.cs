using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Shared.Authorization;
using PhysioAssist.Api.Shared.Consts;
using PhysioAssist.Api.Shared.Dtos.Transcription;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.SystemPrompts;

namespace PhysioAssist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController(IAudioTranscriptionService transcriptionService) : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];
    private readonly IAudioTranscriptionService _transcriptionService = transcriptionService;

    [HttpGet(Name = "GetWeatherForecast")]
    [HasPermission(Permissions.GetUsers)]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
    [HttpPost("voice-text")]
    [RequestSizeLimit(25 * 1024 * 1024)] // 25 MB, adjust to your needs
    public async Task<IActionResult> Transcribe(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return Problem(
                title: "Transcription.EmptyFile",
                detail: "Uploaded file is empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await using var stream = file.OpenReadStream();

        var whisperHint = "جلسة تقييم أولي علاج طبيعي. الطبيب يصف حالة المريض والتشخيص والتاريخ المرضي.";

        var result = await _transcriptionService.TranscribeAsync(new TranscriptionRequest(stream, file.FileName, null, whisperHint), cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        return Ok(new
        {
            raw = result.Value.RawText,
            refined = result.Value.RefinedText
        });
    }

    
}
