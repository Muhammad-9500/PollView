using System.ComponentModel.DataAnnotations;

namespace PollViews.Entities;

public sealed class SurveyPoliticalPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SurveyResponseId { get; set; }
    public SurveyResponse SurveyResponse { get; set; } = null!;

    public SurveyOffice Office { get; set; }

    [Required, MaxLength(200)]
    public string CandidateName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Constituency { get; set; }

    public ICollection<SurveyReason> Reasons { get; set; } = [];
}
