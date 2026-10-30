using Microsoft.EntityFrameworkCore;
using PollViews.Data;
using PollViews.Entities;

namespace PollViews.Services;

public sealed class DemoResponseSeeder(PollViewsDbContext db, ILogger<DemoResponseSeeder> logger)
{
    private const string Marker = "Seeded dashboard demo response";
    private const int TargetCount = 100;

    private static readonly string[] PresidentialCandidates =
    [
        "Bola Ahmed Tinubu (APC)",
        "Atiku Abubakar (ADC)",
        "Peter Gregory Obi (NDC)",
        "Sandy Ojang Onor (PDP)",
        "Undecided"
    ];

    private static readonly string[] Reasons =
    [
        "Good track record",
        "Trustworthy and honest",
        "Economic development",
        "Poor performance",
        "Not known to me"
    ];

    private static readonly string[] Issues =
    [
        "Economic Development",
        "Security",
        "Human Capital Development",
        "Infrastructural Development"
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingCount = await db.SurveyResponses.CountAsync(response => response.AdditionalComment == Marker, cancellationToken);
        if (existingCount >= TargetCount)
        {
            logger.LogInformation("Dashboard demo responses already exist: {Count} records.", existingCount);
            return;
        }

        var locations = await LoadLocationsAsync(cancellationToken);
        if (locations.Count == 0) throw new InvalidOperationException("Cannot seed demo responses because no polling units exist.");

        var needed = TargetCount - existingCount;
        var responses = new List<SurveyResponse>(needed);
        var startDate = DateTimeOffset.UtcNow.AddDays(-30);

        for (var index = 0; index < needed; index++)
        {
            var location = locations[index % locations.Count];
            var pattern = (existingCount + index) % 5;
            var candidate = PresidentialCandidates[pattern];
            var reason = Reasons[pattern];
            var submittedAt = startDate.AddDays((existingCount + index) % 30).AddHours(index % 12);
            var response = new SurveyResponse
            {
                Language = index % 4 == 0 ? "ha" : "en",
                Status = SurveyResponseStatus.Submitted,
                StateId = location.StateId,
                LocalGovernmentAreaId = location.LgaId,
                WardId = location.WardId,
                PollingUnitId = location.PollingUnitId,
                FullName = $"Demo Respondent {existingCount + index + 1:000}",
                EmailAddress = $"demo{existingCount + index + 1:000}@example.test",
                PhoneNumber = $"0800000{existingCount + index + 1:0000}",
                AgeGroup = index % 3 == 0 ? "18-30" : index % 3 == 1 ? "31-45" : "46-59",
                OwnsVotersCard = index % 4 != 0,
                PreferredPoliticalParty = candidate.Contains("(") ? candidate[(candidate.IndexOf('(') + 1)..].TrimEnd(')') : "Undecided",
                MostImportantElectionIssue = Issues[index % Issues.Length],
                CouldAnythingChangeChoice = index % 2 == 0,
                WhatCouldChangeChoice = index % 2 == 0 ? "A clear plan and visible delivery." : null,
                ElectionImportance = index % 3 == 0 ? "Very Important" : "Important",
                AdditionalComment = Marker,
                CreatedAt = submittedAt,
                SubmittedAt = submittedAt
            };

            AddPreference(response, SurveyOffice.Presidential, candidate, reason);
            AddPreference(response, SurveyOffice.Governorship, $"Demo governorship candidate {(index % 4) + 1} ({response.PreferredPoliticalParty})", reason);
            AddPreference(response, SurveyOffice.Senate, $"Demo senate candidate {(index % 4) + 1} ({response.PreferredPoliticalParty})", reason);
            AddPreference(response, SurveyOffice.HouseOfRepresentatives, $"Demo representative candidate {(index % 4) + 1} ({response.PreferredPoliticalParty})", reason);
            AddPreference(response, SurveyOffice.HouseOfAssembly, $"Demo assembly candidate {(index % 4) + 1} ({response.PreferredPoliticalParty})", reason);
            responses.Add(response);
        }

        db.SurveyResponses.AddRange(responses);
        await db.SaveChangesAsync(cancellationToken);

        db.AdminReviews.AddRange(responses.Select(response => new AdminReview
        {
            SurveyResponseId = response.Id,
            Status = AdminReviewStatus.Approved,
            Note = "Approved seeded dashboard demo response.",
            ReviewedAt = response.SubmittedAt
        }));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} approved dashboard demo responses.", responses.Count);
    }

    private async Task<List<DemoLocation>> LoadLocationsAsync(CancellationToken cancellationToken)
    {
        var states = await db.States.AsNoTracking().OrderBy(state => state.Id).ToListAsync(cancellationToken);
        var locations = new List<DemoLocation>();
        foreach (var state in states)
        {
            var stateLocations = await db.PollingUnits
                .AsNoTracking()
                .Where(unit => unit.Ward.LocalGovernmentArea.StateId == state.Id)
                .OrderBy(unit => unit.Id)
                .Take(4)
                .Select(unit => new DemoLocation(
                    state.Id,
                    unit.Ward.LocalGovernmentAreaId,
                    unit.WardId,
                    unit.Id,
                    state.Name,
                    unit.Ward.LocalGovernmentArea.Name,
                    unit.Ward.Name,
                    unit.Name))
                .ToListAsync(cancellationToken);
            locations.AddRange(stateLocations);
        }

        return locations;
    }

    private static void AddPreference(SurveyResponse response, SurveyOffice office, string candidate, string reason)
    {
        var preference = new SurveyPoliticalPreference
        {
            SurveyResponse = response,
            Office = office,
            CandidateName = candidate
        };
        response.PoliticalPreferences.Add(preference);
        preference.Reasons.Add(new SurveyReason
        {
            SurveyResponse = response,
            SurveyPoliticalPreference = preference,
            Type = SurveyReasonType.Preferred,
            Reason = reason
        });
    }

    private sealed record DemoLocation(int StateId, int LgaId, int WardId, int PollingUnitId, string StateName, string LgaName, string WardName, string PollingUnitName);
}
