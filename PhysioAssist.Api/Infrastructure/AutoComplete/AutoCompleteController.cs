using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Infrastructure.AutoComplete.Models;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Infrastructure.AutoComplete
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutoCompleteController : ControllerBase
    {
        private readonly IAutoCompleteService _service;

        public AutoCompleteController(IAutoCompleteService service)
        {
            _service = service;
        }

        [HttpGet("suggest")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "prefix", "limit" })]
        public ActionResult<IReadOnlyList<Suggestion>> Suggest(
            [FromQuery] string prefix,
            [FromQuery] int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return Ok(Array.Empty<Suggestion>());

            var suggestions = _service.GetSuggestions(prefix, limit);
            return Ok(suggestions);
        }
    }
}
