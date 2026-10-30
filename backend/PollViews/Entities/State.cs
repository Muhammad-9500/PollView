using System.ComponentModel.DataAnnotations;

namespace PollViews.Entities;

public sealed class State
{
    public int Id { get; set; }

    [Required, MaxLength(2)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? InecPortalId { get; set; }

    public ICollection<LocalGovernmentArea> LocalGovernmentAreas { get; set; } = [];
}
