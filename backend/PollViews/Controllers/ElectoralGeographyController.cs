using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PollViews.Data;
using PollViews.DTOs;
using PollViews.Entities;

namespace PollViews.Controllers;

[ApiController]
[Route("api/electoral")]
public sealed class ElectoralGeographyController(PollViewsDbContext db) : ControllerBase
{
    [HttpGet("states")]
    public async Task<ActionResult<IReadOnlyList<GeographyOption>>> States(CancellationToken cancellationToken)
    {
        var states = await db.States
            .AsNoTracking()
            .OrderBy(state => state.Name)
            .Select(state => new GeographyOption(state.Code, state.Name, state.InecPortalId ?? string.Empty))
            .ToListAsync(cancellationToken);

        return Ok(states);
    }

    [HttpGet("lgas")]
    public async Task<ActionResult<IReadOnlyList<GeographyOption>>> Lgas([FromQuery] string state, CancellationToken cancellationToken)
    {
        var stateEntity = await FindStateAsync(state, cancellationToken);
        if (stateEntity is null) return Ok(Array.Empty<GeographyOption>());

        var lgas = await db.LocalGovernmentAreas
            .AsNoTracking()
            .Where(lga => lga.StateId == stateEntity.Id)
            .OrderBy(lga => lga.Name)
            .Select(lga => new GeographyOption(lga.Code, lga.Name, lga.InecPortalId ?? string.Empty))
            .ToListAsync(cancellationToken);

        return Ok(lgas);
    }

    [HttpGet("wards")]
    public async Task<ActionResult<IReadOnlyList<GeographyOption>>> Wards([FromQuery] string state, [FromQuery] string lga, CancellationToken cancellationToken)
    {
        var stateEntity = await FindStateAsync(state, cancellationToken);
        if (stateEntity is null) return Ok(Array.Empty<GeographyOption>());

        var lgaEntity = await FindLgaAsync(stateEntity.Id, lga, cancellationToken);
        if (lgaEntity is null) return Ok(Array.Empty<GeographyOption>());

        var wards = await db.Wards
            .AsNoTracking()
            .Where(ward => ward.LocalGovernmentAreaId == lgaEntity.Id)
            .OrderBy(ward => ward.Name)
            .Select(ward => new GeographyOption(ward.Code, ward.Name, ward.InecPortalId ?? string.Empty))
            .ToListAsync(cancellationToken);

        return Ok(wards);
    }

    [HttpGet("polling-units")]
    public async Task<ActionResult<IReadOnlyList<PollingUnitOption>>> PollingUnits(
        [FromQuery] string state,
        [FromQuery] string lga,
        [FromQuery] string ward,
        CancellationToken cancellationToken)
    {
        var stateEntity = await FindStateAsync(state, cancellationToken);
        if (stateEntity is null) return Ok(Array.Empty<PollingUnitOption>());

        var lgaEntity = await FindLgaAsync(stateEntity.Id, lga, cancellationToken);
        if (lgaEntity is null) return Ok(Array.Empty<PollingUnitOption>());

        var wardEntity = await FindWardAsync(lgaEntity.Id, ward, cancellationToken);
        if (wardEntity is null) return Ok(Array.Empty<PollingUnitOption>());

        var pollingUnits = await db.PollingUnits
            .AsNoTracking()
            .Where(pollingUnit => pollingUnit.WardId == wardEntity.Id)
            .OrderBy(pollingUnit => pollingUnit.Code)
            .Select(pollingUnit => new PollingUnitOption(
                pollingUnit.Code,
                pollingUnit.Name,
                pollingUnit.Location ?? string.Empty,
                pollingUnit.FullCode,
                pollingUnit.InecPortalId ?? string.Empty))
            .ToListAsync(cancellationToken);

        return Ok(pollingUnits);
    }

    private async Task<State?> FindStateAsync(string value, CancellationToken cancellationToken)
    {
        var normalizedValue = value.Trim();
        return await db.States
            .AsNoTracking()
            .FirstOrDefaultAsync(state =>
                EF.Functions.ILike(state.Code, normalizedValue) ||
                (state.InecPortalId != null && EF.Functions.ILike(state.InecPortalId, normalizedValue)) ||
                EF.Functions.ILike(state.Name, normalizedValue), cancellationToken);
    }

    private async Task<LocalGovernmentArea?> FindLgaAsync(int stateId, string value, CancellationToken cancellationToken)
    {
        var normalizedValue = value.Trim();
        return await db.LocalGovernmentAreas
            .AsNoTracking()
            .Where(lga => lga.StateId == stateId)
            .FirstOrDefaultAsync(lga =>
                EF.Functions.ILike(lga.Code, normalizedValue) ||
                (lga.InecPortalId != null && EF.Functions.ILike(lga.InecPortalId, normalizedValue)) ||
                EF.Functions.ILike(lga.Name, normalizedValue), cancellationToken);
    }

    private async Task<Ward?> FindWardAsync(int lgaId, string value, CancellationToken cancellationToken)
    {
        var normalizedValue = value.Trim();
        return await db.Wards
            .AsNoTracking()
            .Where(ward => ward.LocalGovernmentAreaId == lgaId)
            .FirstOrDefaultAsync(ward =>
                EF.Functions.ILike(ward.Code, normalizedValue) ||
                (ward.InecPortalId != null && EF.Functions.ILike(ward.InecPortalId, normalizedValue)) ||
                EF.Functions.ILike(ward.Name, normalizedValue), cancellationToken);
    }
}
