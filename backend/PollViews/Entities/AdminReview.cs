using System.ComponentModel.DataAnnotations;

namespace PollViews.Entities;

public sealed class AdminReview
{
    public Guid SurveyResponseId { get; set; }
    public SurveyResponse SurveyResponse { get; set; } = null!;

    public AdminReviewStatus Status { get; set; } = AdminReviewStatus.Pending;

    [MaxLength(2000)]
    public string? Note { get; set; }

    public int? ReviewedByAdminUserId { get; set; }
    public AdminUser? ReviewedByAdminUser { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}
