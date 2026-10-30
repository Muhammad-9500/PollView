using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PollViews.Data;
using PollViews.DTOs;
using PollViews.Entities;
using PollViews.Services;

namespace PollViews.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminDashboardController(
    PollViewsDbContext db,
    SentimentAnalysisService sentiment) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardResponse>> Dashboard(CancellationToken cancellationToken)
    {
        var responses = await db.SurveyResponses
            .AsNoTracking()
            .Where(response => response.Status == SurveyResponseStatus.Submitted)
            .Include(response => response.State)
            .Include(response => response.LocalGovernmentArea)
            .Include(response => response.Ward)
            .OrderByDescending(response => response.SubmittedAt)
            .ToListAsync(cancellationToken);

        var responseIds = responses.Select(response => response.Id).ToList();
        var reviews = await db.AdminReviews
            .AsNoTracking()
            .Where(review => responseIds.Contains(review.SurveyResponseId))
            .ToDictionaryAsync(review => review.SurveyResponseId, cancellationToken);

        var pending = responses.Count(response => !reviews.TryGetValue(response.Id, out var review) || review.Status == AdminReviewStatus.Pending);
        var approved = reviews.Values.Count(review => review.Status == AdminReviewStatus.Approved);
        var flagged = reviews.Values.Count(review => review.Status == AdminReviewStatus.Flagged);
        var rejected = reviews.Values.Count(review => review.Status == AdminReviewStatus.Rejected);

        var preferences = await db.SurveyPoliticalPreferences
            .AsNoTracking()
            .Where(preference => responseIds.Contains(preference.SurveyResponseId))
            .Select(preference => new PreferenceRow(preference.Id, preference.SurveyResponseId, preference.Office, preference.CandidateName))
            .ToListAsync(cancellationToken);
        var reasons = await db.SurveyReasons
            .AsNoTracking()
            .Where(reason => responseIds.Contains(reason.SurveyResponseId))
            .Select(reason => new ReasonRow(reason.SurveyResponseId, reason.SurveyPoliticalPreferenceId, reason.Reason))
            .ToListAsync(cancellationToken);
        var responseText = responses.ToDictionary(response => response.Id, response => new[]
        {
            response.AdditionalComment,
            response.WhatCouldChangeChoice
        }.Where(text => !string.IsNullOrWhiteSpace(text)).Cast<string>().ToList());

        var candidateInsights = BuildCandidateInsights(preferences, reasons, responseText);
        var recent = responses.Take(100).Select(response => new AdminSubmissionRow(
            response.Id,
            response.SubmittedAt ?? response.CreatedAt,
            response.FullName,
            response.State.Name,
            response.LocalGovernmentArea.Name,
            response.Ward.Name,
            response.PreferredPoliticalParty,
            GetReviewStatus(reviews.GetValueOrDefault(response.Id)))).ToList();

        var trends = responses
            .GroupBy(response => (response.SubmittedAt ?? response.CreatedAt).ToLocalTime().Date)
            .OrderBy(group => group.Key)
            .Select(group => new SubmissionTrend(group.Key.ToString("yyyy-MM-dd"), group.Count()))
            .ToList();
        var stateBreakdown = responses
            .GroupBy(response => response.State.Name)
            .OrderByDescending(group => group.Count())
            .Select(group => new CategoryCount(group.Key, group.Count()))
            .ToList();
        var issueBreakdown = responses
            .Where(response => !string.IsNullOrWhiteSpace(response.MostImportantElectionIssue))
            .GroupBy(response => response.MostImportantElectionIssue!)
            .OrderByDescending(group => group.Count())
            .Take(10)
            .Select(group => new CategoryCount(group.Key, group.Count()))
            .ToList();

        return Ok(new AdminDashboardResponse(
            responses.Count,
            pending,
            approved,
            flagged,
            rejected,
            trends,
            stateBreakdown,
            issueBreakdown,
            candidateInsights,
            recent));
    }

    [HttpGet("submissions/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var response = await db.SurveyResponses
            .AsNoTracking()
            .Include(item => item.State)
            .Include(item => item.LocalGovernmentArea)
            .Include(item => item.Ward)
            .Include(item => item.PollingUnit)
            .Include(item => item.PoliticalPreferences)
                .ThenInclude(preference => preference.Reasons)
            .Include(item => item.Reasons)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (response is null) return NotFound();

        var review = await db.AdminReviews.AsNoTracking().FirstOrDefaultAsync(item => item.SurveyResponseId == id, cancellationToken);
        return Ok(new
        {
            response.Id,
            response.Language,
            response.FullName,
            response.EmailAddress,
            response.PhoneNumber,
            response.AgeGroup,
            response.OwnsVotersCard,
            response.PreferredPoliticalParty,
            response.MostImportantElectionIssue,
            response.CouldAnythingChangeChoice,
            response.WhatCouldChangeChoice,
            response.ElectionImportance,
            response.AdditionalComment,
            response.CreatedAt,
            response.SubmittedAt,
            location = new { State = response.State.Name, Lga = response.LocalGovernmentArea.Name, Ward = response.Ward.Name, PollingUnit = response.PollingUnit.Name },
            preferences = response.PoliticalPreferences.Select(preference => new
            {
                office = preference.Office.ToString(),
                candidate = preference.CandidateName,
                reasons = preference.Reasons.Select(reason => reason.Reason)
            }),
            reasons = response.Reasons.Where(reason => reason.SurveyPoliticalPreferenceId == null).Select(reason => new { type = reason.Type.ToString(), reason = reason.Reason }),
            review = review is null ? new { status = "Pending", note = (string?)null, reviewedAt = (DateTimeOffset?)null } : new { status = review.Status.ToString(), note = review.Note, reviewedAt = review.ReviewedAt }
        });
    }

    [HttpPut("submissions/{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, AdminReviewRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AdminReviewStatus>(request.Status, true, out var status))
            return BadRequest(new { message = "Invalid review status." });

        if (!await db.SurveyResponses.AnyAsync(response => response.Id == id, cancellationToken))
            return NotFound();

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var review = await db.AdminReviews.FirstOrDefaultAsync(item => item.SurveyResponseId == id, cancellationToken);
        if (review is null)
        {
            review = new AdminReview { SurveyResponseId = id };
            db.AdminReviews.Add(review);
        }

        review.Status = status;
        review.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        review.ReviewedByAdminUserId = adminId;
        review.ReviewedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = review.Status.ToString(), reviewedAt = review.ReviewedAt });
    }

    private List<CandidateInsight> BuildCandidateInsights(
        IReadOnlyList<PreferenceRow> preferences,
        IReadOnlyList<ReasonRow> reasons,
        IReadOnlyDictionary<Guid, List<string>> responseText)
    {
        var reasonLookup = reasons
            .Where(reason => reason.SurveyPoliticalPreferenceId is not null)
            .GroupBy(reason => reason.SurveyPoliticalPreferenceId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(reason => reason.Reason).ToList());

        var groups = preferences.GroupBy(preference => new { preference.Office, preference.CandidateName });
        return groups.Select(group =>
        {
            var positive = 0;
            var neutral = 0;
            var negative = 0;
            var allReasons = new List<string>();
            foreach (var preference in group)
            {
                reasonLookup.TryGetValue(preference.Id, out var preferenceReasons);
                var messages = preferenceReasons ?? [];
                allReasons.AddRange(messages);
                if (responseText.TryGetValue(preference.SurveyResponseId, out var comments)) messages.AddRange(comments);
                var result = sentiment.Analyze(messages);
                if (result.Label == "Positive") positive++;
                else if (result.Label == "Negative") negative++;
                else neutral++;
            }

            return new CandidateInsight(
                group.Key.Office.ToString(),
                group.Key.CandidateName,
                group.Count(),
                positive,
                neutral,
                negative,
                allReasons.Where(reason => !string.IsNullOrWhiteSpace(reason))
                    .GroupBy(reason => reason)
                    .OrderByDescending(reason => reason.Count())
                    .Take(5)
                    .Select(reason => reason.Key)
                    .ToList());
        }).OrderByDescending(insight => insight.Responses).ToList();
    }

    private static string GetReviewStatus(AdminReview? review) => review?.Status.ToString() ?? AdminReviewStatus.Pending.ToString();

    private sealed record PreferenceRow(Guid Id, Guid SurveyResponseId, SurveyOffice Office, string CandidateName);
    private sealed record ReasonRow(Guid SurveyResponseId, Guid? SurveyPoliticalPreferenceId, string Reason);
}
