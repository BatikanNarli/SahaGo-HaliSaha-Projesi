using System.Globalization;
using Halisaha.Application.Availability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Halisaha.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/availability")]
[EnableRateLimiting("availability")]
public sealed class AvailabilityController(IAvailabilitySearchService searchService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<AvailableField>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string district,
        [FromQuery] string localDate,
        [FromQuery] string localTime,
        [FromQuery] int durationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        var transportErrors = new Dictionary<string, string[]>();
        if (!DateOnly.TryParseExact(localDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            transportErrors["localDate"] = ["Tarih yyyy-MM-dd biçiminde olmalıdır."];
        }

        if (!TimeOnly.TryParseExact(localTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
        {
            transportErrors["localTime"] = ["Saat HH:mm biçiminde olmalıdır."];
        }

        if (transportErrors.Count > 0)
        {
            return Invalid("Arama kriterleri geçersiz.", transportErrors);
        }

        var result = await searchService.SearchAsync(
            new AvailabilitySearchQuery(district, parsedDate, parsedTime, durationMinutes),
            cancellationToken);
        return result.Succeeded
            ? Ok(result.Fields)
            : Invalid(result.Failure!.Message, result.Failure.Errors);
    }

    private BadRequestObjectResult Invalid(string title, IReadOnlyDictionary<string, string[]> errors)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = title,
            Type = "https://httpstatuses.com/400"
        };
        problem.Extensions["errors"] = errors;
        problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        return BadRequest(problem);
    }
}
