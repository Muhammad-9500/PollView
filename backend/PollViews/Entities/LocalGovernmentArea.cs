using System.ComponentModel.DataAnnotations;

namespace PollViews.Entities;

public sealed class LocalGovernmentArea
{
    public int Id { get; set; }

    public int StateId { get; set; }
    public State State { get; set; } = null!;

    [Required, MaxLength(2)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? InecPortalId { get; set; }

    public ICollection<Ward> Wards { get; set; } = [];
}
