using System.ComponentModel.DataAnnotations;

namespace PollViews.Entities;

public sealed class SurveyReason
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SurveyResponseId { get; set; }
    public SurveyResponse SurveyResponse { get; set; } = null!;

    public Guid? SurveyPoliticalPreferenceId { get; set; }
    public SurveyPoliticalPreference? SurveyPoliticalPreference { get; set; }

    public SurveyReasonType Type { get; set; }

    [Required, MaxLength(300)]
    public string Reason { get; set; } = string.Empty;
}
