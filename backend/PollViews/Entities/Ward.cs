using System.ComponentModel.DataAnnotations;

namespace PollViews.Entities;

public sealed class Ward
{
    public int Id { get; set; }

    public int LocalGovernmentAreaId { get; set; }
    public LocalGovernmentArea LocalGovernmentArea { get; set; } = null!;

    [Required, MaxLength(2)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? InecPortalId { get; set; }

    public ICollection<PollingUnit> PollingUnits { get; set; } = [];
}
