using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinimalTaxiService.Application.Trips.Commands;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Application.Trips.Queries;
using MinimalTaxiService.Contracts.Trips;
using MinimalTaxiService.Domain.Enums;
using MinimalTaxiService.Web.Extensions;
using Shared;
using System.Text.Json;

namespace MinimalTaxiService.Web.Controllers;

[ApiController]
[Route("trips")]
[Authorize]
public class TripsController : ControllerBase
{
    [HttpGet("events")]
    public async Task Events(
        [FromServices] ITripEventsBus tripEventsBus,
        CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.Append("X-Accel-Buffering", "no");

        await Response.WriteAsync(": connected\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        await foreach (var tripEvent in tripEventsBus.Subscribe(cancellationToken))
        {
            var payload = JsonSerializer.Serialize(tripEvent);
            var eventName = IsInvitationEvent(tripEvent.EventType)
                ? "intercity.invitations.changed"
                : "trips.changed";

            await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    private static bool IsInvitationEvent(string? eventType)
    {
        return eventType?.StartsWith("invitation.", StringComparison.OrdinalIgnoreCase) == true;
    }

    [HttpPost("local")]
    public async Task<IActionResult> CreateLocal(
        [FromBody] CreateLocalTripRequest request,
        [FromServices] CreateLocalTripCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var userRole = User.GetUserRole();
        if (!userRole.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User role claim is missing")));

        if (userRole.Value == UserRole.Driver)
            return BadRequest(Envelope.Error(Error.Validation("operation.is.forbidden", "Driver cannot create local trip requests")));

        var command = new CreateLocalTripCommand(
            userId.Value,
            request.From.Latitude,
            request.From.Longitude,
            request.To.Latitude,
            request.To.Longitude,
            request.FromAddress,
            request.ToAddress,
            request.Description);

        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok(result.Value));
    }

    [HttpPost("intercity")]
    public async Task<IActionResult> CreateIntercity(
        [FromBody] CreateIntercityTripRequest request,
        [FromServices] CreateIntercityTripCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var userRole = User.GetUserRole();
        if (!userRole.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User role claim is missing")));

        var createdByDriver = userRole.Value == UserRole.Driver;

        var command = new CreateIntercityTripCommand(
            userId.Value,
            createdByDriver,
            request.From.Latitude,
            request.From.Longitude,
            request.To.Latitude,
            request.To.Longitude,
            request.FromAddress,
            request.ToAddress,
            request.DepartureAt,
            request.Description,
            request.TotalSeats,
            request.RequiredSeats);

        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok(result.Value));
    }

    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<IActionResult> Nearby(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] int radiusMeters,
        [FromQuery] string? city,
        [FromQuery] string? fromAddress,
        [FromQuery] string? toAddress,
        [FromQuery] double? fromLatitude,
        [FromQuery] double? fromLongitude,
        [FromQuery] int? fromRadiusMeters,
        [FromQuery] double? toLatitude,
        [FromQuery] double? toLongitude,
        [FromQuery] int? toRadiusMeters,
        [FromQuery] TripType? tripType,
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] bool includeInactive,
        [FromServices] GetNearbyTripsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetNearbyTripsQuery(
            latitude,
            longitude,
            radiusMeters,
            city,
            fromAddress,
            toAddress,
            fromLatitude,
            fromLongitude,
            fromRadiusMeters,
            toLatitude,
            toLongitude,
            toRadiusMeters,
            tripType,
            limit,
            offset,
            includeInactive);
        var result = await handler.Handle(query, cancellationToken);
        return Ok(Envelope.Ok(result));
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyTrips(
        [FromQuery] bool onlyActive,
        [FromServices] GetUserTripsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(new GetUserTripsQuery(userId.Value, onlyActive), cancellationToken);
        return Ok(Envelope.Ok(result));
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(
        [FromRoute] Guid id,
        [FromBody] AcceptTripRequest request,
        [FromServices] AcceptTripCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(new AcceptTripCommand(id, userId.Value, request.TotalSeats), cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok());
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelTripRequest request,
        [FromServices] CancelTripCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(new CancelTripCommand(id, userId.Value), cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok(new { request.Reason }));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(
        [FromRoute] Guid id,
        [FromServices] CompleteTripCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(Envelope.Error(Error.Validation("auth.invalid", "User id claim is missing")));

        var result = await handler.Handle(new CompleteTripCommand(id, userId.Value), cancellationToken);
        if (result.IsFailure)
            return this.ToHttpResult(result.Error);

        return Ok(Envelope.Ok());
    }
}
