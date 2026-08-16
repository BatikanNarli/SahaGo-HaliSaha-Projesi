using System.Security.Claims;
using Halisaha.Application.Identity;
using Halisaha.Application.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Halisaha.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthRoles.Customer)]
[EnableRateLimiting("reservations")]
[Route("api/v1/reservations")]
public sealed class ReservationsController(IReservationService reservationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ReservationView>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReservationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await reservationService.CreateAsync(
            CurrentUserId(),
            new CreateReservationCommand(request.FieldId, request.StartsAtUtc, request.EndsAtUtc, idempotencyKey ?? string.Empty),
            HttpContext.TraceIdentifier,
            cancellationToken);
        if (!result.Succeeded)
        {
            return FromFailure(result.Failure!);
        }

        return result.Replayed
            ? Ok(result.Reservation)
            : CreatedAtAction(nameof(GetById), new { reservationId = result.Reservation!.Id }, result.Reservation);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ReservationView>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            return FromFailure(new ReservationFailure(
                ReservationFailureCode.Validation,
                "Sayfalama bilgileri geçersiz.",
                new Dictionary<string, string[]> { ["pageSize"] = ["page en az 1, pageSize 1 ile 100 arasında olmalıdır."] }));
        }

        return Ok(await reservationService.ListMineAsync(CurrentUserId(), page, pageSize, cancellationToken));
    }

    [HttpGet("{reservationId:guid}")]
    [ProducesResponseType<ReservationView>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid reservationId, CancellationToken cancellationToken)
    {
        var result = await reservationService.GetMineAsync(CurrentUserId(), reservationId, cancellationToken);
        return result.Succeeded ? Ok(result.Reservation) : FromFailure(result.Failure!);
    }

    [HttpPost("{reservationId:guid}/cancel")]
    [ProducesResponseType<ReservationView>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(
        Guid reservationId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await reservationService.CancelAsync(
            CurrentUserId(),
            new CancelReservationCommand(reservationId, idempotencyKey ?? string.Empty),
            HttpContext.TraceIdentifier,
            cancellationToken);
        return result.Succeeded ? Ok(result.Reservation) : FromFailure(result.Failure!);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private ObjectResult FromFailure(ReservationFailure failure)
    {
        var status = failure.Code switch
        {
            ReservationFailureCode.Validation => StatusCodes.Status400BadRequest,
            ReservationFailureCode.NotFound => StatusCodes.Status404NotFound,
            ReservationFailureCode.Conflict or ReservationFailureCode.IdempotencyConflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        var details = new ProblemDetails
        {
            Status = status,
            Title = failure.Message,
            Type = $"https://httpstatuses.com/{status}"
        };
        details.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        if (failure.Errors is not null)
        {
            details.Extensions["errors"] = failure.Errors;
        }

        return StatusCode(status, details);
    }
}

public sealed record CreateReservationRequest(Guid FieldId, DateTime StartsAtUtc, DateTime EndsAtUtc);
