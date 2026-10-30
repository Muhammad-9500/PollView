using System.ComponentModel.DataAnnotations;

namespace PollViews.Entities;

public sealed class PollingUnit
{
    public int Id { get; set; }

    public int WardId { get; set; }
    public Ward Ward { get; set; } = null!;

    [Required, MaxLength(3)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Location { get; set; }

    [Required, MaxLength(20)]
    public string FullCode { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? InecPortalId { get; set; }

    public ICollection<SurveyResponse> SurveyResponses { get; set; } = [];
}
