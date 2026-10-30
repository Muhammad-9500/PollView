namespace PollViews.DTOs;

public sealed record AdminLoginRequest(string Username, string Password);

public sealed record CandidateInsight(
    string Office,
    string Candidate,
    int Responses,
    int Positive,
    int Neutral,
    int Negative,
    IReadOnlyList<string> TopReasons);

public sealed record SubmissionTrend(string Date, int Count);
public sealed record CategoryCount(string Label, int Count);

public sealed record AdminSubmissionRow(
    Guid Id,
    DateTimeOffset SubmittedAt,
    string FullName,
    string State,
    string LocalGovernmentArea,
    string Ward,
    string PreferredPoliticalParty,
    string ReviewStatus);

public sealed record AdminDashboardResponse(
    int TotalSubmissions,
    int PendingReviews,
    int ApprovedReviews,
    int FlaggedReviews,
    int RejectedReviews,
    IReadOnlyList<SubmissionTrend> SubmissionTrends,
    IReadOnlyList<CategoryCount> StateBreakdown,
    IReadOnlyList<CategoryCount> IssueBreakdown,
    IReadOnlyList<CandidateInsight> CandidateInsights,
    IReadOnlyList<AdminSubmissionRow> RecentSubmissions);

public sealed record AdminReviewRequest(string Status, string? Note);
