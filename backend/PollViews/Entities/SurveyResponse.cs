using System.ComponentModel.DataAnnotations;

namespace PollViews.Entities;

public sealed class SurveyResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(5)]
    public string Language { get; set; } = "en";

    public SurveyResponseStatus Status { get; set; } = SurveyResponseStatus.Draft;

    public int StateId { get; set; }
    public State State { get; set; } = null!;

    public int LocalGovernmentAreaId { get; set; }
    public LocalGovernmentArea LocalGovernmentArea { get; set; } = null!;

    public int WardId { get; set; }
    public Ward Ward { get; set; } = null!;

    public int PollingUnitId { get; set; }
    public PollingUnit PollingUnit { get; set; } = null!;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(320), EmailAddress]
    public string? EmailAddress { get; set; }

    [Required, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string AgeGroup { get; set; } = string.Empty;

    public bool OwnsVotersCard { get; set; }

    [Required, MaxLength(40)]
    public string PreferredPoliticalParty { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MostImportantElectionIssue { get; set; }

    public bool? CouldAnythingChangeChoice { get; set; }

    [MaxLength(4000)]
    public string? WhatCouldChangeChoice { get; set; }

    [MaxLength(60)]
    public string? ElectionImportance { get; set; }

    [MaxLength(4000)]
    public string? AdditionalComment { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }

    public ICollection<SurveyPoliticalPreference> PoliticalPreferences { get; set; } = [];
    public ICollection<SurveyReason> Reasons { get; set; } = [];
}
