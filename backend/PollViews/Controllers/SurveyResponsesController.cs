using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PollViews.Data;
using PollViews.DTOs;
using PollViews.Entities;

namespace PollViews.Controllers;

[ApiController]
[Route("api/survey-responses")]
public sealed class SurveyResponsesController(PollViewsDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitSurveyRequest request, CancellationToken cancellationToken)
    {
        var state = await FindStateAsync(request.State, cancellationToken);
        if (state is null) return BadRequest(new { message = "The selected state could not be found." });

        var lga = await FindLgaAsync(state.Id, request.LocalGovernmentArea, cancellationToken);
        if (lga is null) return BadRequest(new { message = "The selected LGA could not be found." });

        var ward = await FindWardAsync(lga.Id, request.Ward, cancellationToken);
        if (ward is null) return BadRequest(new { message = "The selected ward could not be found." });

        var pollingUnit = await db.PollingUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(unit => unit.WardId == ward.Id &&
                (EF.Functions.ILike(unit.FullCode, request.PollingUnit.Trim()) ||
                 EF.Functions.ILike(unit.Code, request.PollingUnit.Trim()) ||
                 EF.Functions.ILike(unit.Name, request.PollingUnit.Trim())), cancellationToken);
        if (pollingUnit is null) return BadRequest(new { message = "The selected polling unit could not be found." });

        if (!TryParseYesNo(request.OwnsVotersCard, out var ownsVotersCard))
            return BadRequest(new { message = "Please provide a valid voter-card answer." });
        if (!TryParseYesNo(request.CouldAnythingChangeChoice, out var couldAnythingChangeChoice))
            return BadRequest(new { message = "Please provide a valid choice-change answer." });
        if (couldAnythingChangeChoice && string.IsNullOrWhiteSpace(request.WhatCouldChangeChoice))
            return BadRequest(new { message = "Please explain what could change your choice." });

        var response = new SurveyResponse
        {
            Language = request.Language.Equals("Hausa", StringComparison.OrdinalIgnoreCase) ? "ha" : "en",
            Status = SurveyResponseStatus.Submitted,
            StateId = state.Id,
            LocalGovernmentAreaId = lga.Id,
            WardId = ward.Id,
            PollingUnitId = pollingUnit.Id,
            FullName = request.FullName.Trim(),
            EmailAddress = string.IsNullOrWhiteSpace(request.EmailAddress) ? null : request.EmailAddress.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            AgeGroup = request.AgeGroup.Trim(),
            OwnsVotersCard = ownsVotersCard,
            PreferredPoliticalParty = request.PreferredPoliticalParty.Trim(),
            MostImportantElectionIssue = request.MostImportantElectionIssue.Trim(),
            CouldAnythingChangeChoice = couldAnythingChangeChoice,
            WhatCouldChangeChoice = couldAnythingChangeChoice ? request.WhatCouldChangeChoice?.Trim() : null,
            ElectionImportance = request.ElectionImportance.Trim(),
            AdditionalComment = string.IsNullOrWhiteSpace(request.AdditionalComment) ? null : request.AdditionalComment.Trim(),
            SubmittedAt = DateTimeOffset.UtcNow
        };

        AddReasons(response, request.PreferredPoliticalPartyReasons, SurveyReasonType.Preferred);
        AddReasons(response, request.OtherPoliticalPartyReasons, SurveyReasonType.NotPreferred);

        AddPreference(response, SurveyOffice.Presidential, request.PresidentialCandidate, request.PresidentialCandidateReasons);
        AddReasons(response, request.OtherPresidentialCandidateReasons, SurveyReasonType.NotPreferred);
        AddPreference(response, SurveyOffice.Governorship, request.GovernorshipCandidate, request.GovernorshipCandidateReasons);
        AddReasons(response, request.OtherGovernorshipCandidateReasons, SurveyReasonType.NotPreferred);
        AddPreference(response, SurveyOffice.Senate, request.SenateCandidate, request.SenateCandidateReasons);
        AddReasons(response, request.OtherSenateCandidateReasons, SurveyReasonType.NotPreferred);
        AddPreference(response, SurveyOffice.HouseOfRepresentatives, request.HouseOfRepresentativesCandidate, request.HouseOfRepresentativesCandidateReasons);
        AddReasons(response, request.OtherHouseOfRepresentativesCandidateReasons, SurveyReasonType.NotPreferred);
        AddPreference(response, SurveyOffice.HouseOfAssembly, request.HouseOfAssemblyCandidate, request.HouseOfAssemblyCandidateReasons);
        AddReasons(response, request.OtherHouseOfAssemblyCandidateReasons, SurveyReasonType.NotPreferred);

        db.SurveyResponses.Add(response);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { id = response.Id, status = "Submitted" });
    }

    private async Task<State?> FindStateAsync(string value, CancellationToken cancellationToken)
    {
        var normalizedValue = value.Trim();
        return await db.States.AsNoTracking().FirstOrDefaultAsync(state =>
            EF.Functions.ILike(state.Code, normalizedValue) ||
            (state.InecPortalId != null && EF.Functions.ILike(state.InecPortalId, normalizedValue)) ||
            EF.Functions.ILike(state.Name, normalizedValue), cancellationToken);
    }

    private async Task<LocalGovernmentArea?> FindLgaAsync(int stateId, string value, CancellationToken cancellationToken)
    {
        var normalizedValue = value.Trim();
        return await db.LocalGovernmentAreas.AsNoTracking().Where(lga => lga.StateId == stateId).FirstOrDefaultAsync(lga =>
            EF.Functions.ILike(lga.Code, normalizedValue) ||
            (lga.InecPortalId != null && EF.Functions.ILike(lga.InecPortalId, normalizedValue)) ||
            EF.Functions.ILike(lga.Name, normalizedValue), cancellationToken);
    }

    private async Task<Ward?> FindWardAsync(int lgaId, string value, CancellationToken cancellationToken)
    {
        var normalizedValue = value.Trim();
        return await db.Wards.AsNoTracking().Where(ward => ward.LocalGovernmentAreaId == lgaId).FirstOrDefaultAsync(ward =>
            EF.Functions.ILike(ward.Code, normalizedValue) ||
            (ward.InecPortalId != null && EF.Functions.ILike(ward.InecPortalId, normalizedValue)) ||
            EF.Functions.ILike(ward.Name, normalizedValue), cancellationToken);
    }

    private static void AddPreference(SurveyResponse response, SurveyOffice office, string candidate, IEnumerable<string> reasons)
    {
        var preference = new SurveyPoliticalPreference { SurveyResponse = response, Office = office, CandidateName = candidate.Trim() };
        response.PoliticalPreferences.Add(preference);
        AddReasons(response, reasons, SurveyReasonType.Preferred, preference);
    }

    private static void AddReasons(SurveyResponse response, IEnumerable<string>? reasons, SurveyReasonType type, SurveyPoliticalPreference? preference = null)
    {
        foreach (var reason in reasons ?? [])
        {
            if (string.IsNullOrWhiteSpace(reason)) continue;
            response.Reasons.Add(new SurveyReason
            {
                SurveyResponse = response,
                SurveyPoliticalPreference = preference,
                Type = type,
                Reason = reason.Trim()
            });
        }
    }

    private static bool TryParseYesNo(string value, out bool result)
    {
        if (value.Equals("Yes", StringComparison.OrdinalIgnoreCase) || value.Equals("Eh", StringComparison.OrdinalIgnoreCase))
        {
            result = true;
            return true;
        }

        if (value.Equals("No", StringComparison.OrdinalIgnoreCase) || value.Equals("A'a", StringComparison.OrdinalIgnoreCase) || value.Equals("A’", StringComparison.OrdinalIgnoreCase))
        {
            result = false;
            return true;
        }

        result = false;
        return false;
    }
}
