using Domain.Dto;
using Microsoft.EntityFrameworkCore;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Interface;
using Web.Contracts;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StationsController(IStationService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StationResponse>> GetById(Guid id)
    {
        var res = await service.GetByIdAsync(id);
        
        if (res is null) return NotFound();
        return Ok(ToResponse(res));
    }

    // Name of the rate-limit policy applied to AddByCity; configured in Program.cs.
    public const string AddStationPolicy = "add-station";

    [HttpPost("by-city")]
    [EnableRateLimiting(AddStationPolicy)]
    public async Task<ActionResult<StationResponse>> AddByCity(
        [FromBody] AddByCityRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.City))
            return BadRequest("City is required.");

        var station = await service.AddByCityAsync(
            request.City, request.CountryCode, cancellationToken);

        return station is null
            ? NotFound($"No location found for city '{request.City}'"
                       + (request.CountryCode is null ? "." : $" in country '{request.CountryCode}'."))
            : Ok(ToResponse(station));
    }

    [HttpGet]
    public async Task<ActionResult<List<StationResponse>>> GetAll(
        [FromQuery] string? city,
        [FromQuery] string? countryCode)
    {
        var result = await service.GetAllAsync(city, countryCode);
        return Ok(result.Select(ToResponse).ToList());
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PaginatedResult<StationResponse>>> Paged([FromQuery] PaginatedRequest request)
    {
        var page = await service.GetPagedAsync(request.Page, request.Size);

        return Ok(new PaginatedResult<StationResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            TotalCount = page.TotalCount,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalPages = page.TotalPages
        });
    }

    // TODO implement
    /// ADMIN ONLY: stops tracking a city and deletes every reading recorded for it.
    /// Refused while an alert subscription still points at the station.
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<StationResponse>> Delete(Guid id)
    {
        try
        {
            var station = await service.DeleteByIdAsync(id);
            return Ok(ToResponse(station));
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Station '{id}' was not found.");
        }
        catch (DbUpdateException)
        {
            // AlertSubscription -> Station is ON DELETE RESTRICT, so the database refuses
            // rather than orphaning someone's alert. Expected, not a server fault.
            return Conflict($"Station '{id}' still has alert subscriptions and cannot be deleted.");
        }
    }

    private static StationResponse ToResponse(Station s) 
        => new(s.Id, s.City, s.CountryCode, s.Latitude, s.Longitude, s.ExternalId);
}
