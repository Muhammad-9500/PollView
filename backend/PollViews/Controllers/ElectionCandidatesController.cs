using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using PollViews.DTOs;

namespace PollViews.Controllers;

[ApiController]
[Route("api/candidates")]
public sealed class ElectionCandidatesController(
    IHttpClientFactory httpClientFactory,
    ILogger<ElectionCandidatesController> logger) : ControllerBase
{
    private const string CandidateSource = "https://everyvotes.legis360.org/api/campaign/candidates";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ElectionCandidateOption>>> Get(
        [FromQuery] string office,
        [FromQuery] string? state,
        CancellationToken cancellationToken)
    {
        var race = office.Trim().ToLowerInvariant() switch
        {
            "presidential" => "PRES",
            "governorship" => "GOV",
            "senate" => "SEN",
            "house-of-representatives" => "REPS",
            "state-assembly" => "ASSEMBLY",
            _ => null
        };

        if (race is null) return BadRequest("The office is not supported.");
        if (race != "PRES" && string.IsNullOrWhiteSpace(state)) return BadRequest("State is required for this office.");

        var query = new List<string> { $"race={Uri.EscapeDataString(race)}" };
        if (race != "PRES") query.Add($"state={Uri.EscapeDataString(state!.Trim())}");

        try
        {
            var client = httpClientFactory.CreateClient();
            var payload = await client.GetFromJsonAsync<CandidateResponse>(
                $"{CandidateSource}?{string.Join('&', query)}",
                cancellationToken);

            var options = payload?.Candidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Name))
                .OrderBy(candidate => candidate.Constituency ?? string.Empty)
                .ThenBy(candidate => candidate.Party)
                .ThenBy(candidate => candidate.Name)
                .Select(candidate =>
                {
                    var constituency = string.IsNullOrWhiteSpace(candidate.Constituency)
                        ? string.Empty
                        : $" — {candidate.Constituency}";
                    var value = $"{candidate.Name} ({candidate.Party}){constituency}";
                    return new ElectionCandidateOption(value, value);
                })
                .ToList() ?? [];

            options.Add(new ElectionCandidateOption("Undecided", "Undecided"));
            options.Add(new ElectionCandidateOption("Other candidate", "Other candidate"));
            return Ok(options);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Candidate source could not be reached for {Office} in {State}.", office, state);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Candidate data is temporarily unavailable.");
        }
    }

    private sealed class CandidateResponse
    {
        public List<CandidateRecord> Candidates { get; set; } = [];
    }

    private sealed class CandidateRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Party { get; set; } = string.Empty;
        public string? Constituency { get; set; }
    }
}
