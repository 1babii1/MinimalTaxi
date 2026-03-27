using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinimalTaxiService.Domain.ValueObjects;
using MinimalTaxiService.Web.Integrations.Dadata;
using MinimalTaxiService.Web.Integrations.Yandex;
using Shared;

namespace MinimalTaxiService.Web.Controllers;

[ApiController]
[Route("geocoding")]
[Authorize]
public sealed class GeocodingController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest(
        [FromQuery] string query,
        [FromQuery] int limit,
        [FromServices] YandexGeocodingService geocodingService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
            return Ok(Envelope.Ok(Array.Empty<object>()));

        var suggestions = await geocodingService.SuggestAsync(query, limit <= 0 ? 5 : limit, cancellationToken);
        return Ok(Envelope.Ok(suggestions));
    }

    [HttpGet("reverse")]
    public async Task<IActionResult> Reverse(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromServices] DadataGeocodingService geocodingService,
        CancellationToken cancellationToken)
    {
        var locationValidation = Location.Create(latitude, longitude);
        if (locationValidation.IsFailure)
            return BadRequest(Envelope.Error(locationValidation.Error));

        var address = await geocodingService.ReverseAsync(latitude, longitude, cancellationToken);

        if (string.IsNullOrWhiteSpace(address))
            return Ok(Envelope.Ok(new { address = (string?)null }));

        return Ok(Envelope.Ok(new { address }));
    }

    [HttpGet("cars/brands")]
    public async Task<IActionResult> SuggestCarBrands(
        [FromQuery] string query,
        [FromQuery] int limit,
        [FromServices] DadataGeocodingService geocodingService,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery) || normalizedQuery.Length < 1)
            return Ok(Envelope.Ok(Array.Empty<string>()));

        var suggestions = await geocodingService.SuggestCarBrandsAsync(
            normalizedQuery,
            limit <= 0 ? 8 : limit,
            cancellationToken);

        return Ok(Envelope.Ok(suggestions));
    }

    [HttpGet("cars/models")]
    public async Task<IActionResult> SuggestCarModels(
        [FromQuery] string brand,
        [FromQuery] string query,
        [FromQuery] int limit,
        [FromServices] DadataGeocodingService geocodingService,
        CancellationToken cancellationToken)
    {
        var normalizedBrand = brand?.Trim();
        var normalizedQuery = query?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedBrand))
            return Ok(Envelope.Ok(Array.Empty<string>()));

        if (string.IsNullOrWhiteSpace(normalizedQuery) || normalizedQuery.Length < 1)
            return Ok(Envelope.Ok(Array.Empty<string>()));

        var suggestions = await geocodingService.SuggestCarModelsAsync(
            normalizedBrand,
            normalizedQuery,
            limit <= 0 ? 8 : limit,
            cancellationToken);

        return Ok(Envelope.Ok(suggestions));
    }
}
