using System.ComponentModel.DataAnnotations;

namespace PollViews.DTOs;

public sealed class SubmitSurveyRequest
{
    [Required, MaxLength(20)]
    public string Language { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    public string LocalGovernmentArea { get; set; } = string.Empty;

    [Required]
    public string Ward { get; set; } = string.Empty;

    [Required]
    public string PollingUnit { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(320), EmailAddress]
    public string? EmailAddress { get; set; }

    [Required, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string AgeGroup { get; set; } = string.Empty;

    [Required]
    public string OwnsVotersCard { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string PreferredPoliticalParty { get; set; } = string.Empty;

    public List<string> PreferredPoliticalPartyReasons { get; set; } = [];
    public List<string> OtherPoliticalPartyReasons { get; set; } = [];

    [Required, MaxLength(200)]
    public string PresidentialCandidate { get; set; } = string.Empty;
    public List<string> PresidentialCandidateReasons { get; set; } = [];

    public List<string> OtherPresidentialCandidateReasons { get; set; } = [];

    [Required, MaxLength(200)]
    public string GovernorshipCandidate { get; set; } = string.Empty;
    public List<string> GovernorshipCandidateReasons { get; set; } = [];

    public List<string> OtherGovernorshipCandidateReasons { get; set; } = [];

    [Required, MaxLength(200)]
    public string SenateCandidate { get; set; } = string.Empty;
    public List<string> SenateCandidateReasons { get; set; } = [];

    public List<string> OtherSenateCandidateReasons { get; set; } = [];

    [Required, MaxLength(200)]
    public string HouseOfRepresentativesCandidate { get; set; } = string.Empty;
    public List<string> HouseOfRepresentativesCandidateReasons { get; set; } = [];

    public List<string> OtherHouseOfRepresentativesCandidateReasons { get; set; } = [];

    [Required, MaxLength(200)]
    public string HouseOfAssemblyCandidate { get; set; } = string.Empty;
    public List<string> HouseOfAssemblyCandidateReasons { get; set; } = [];

    public List<string> OtherHouseOfAssemblyCandidateReasons { get; set; } = [];

    [Required, MaxLength(100)]
    public string MostImportantElectionIssue { get; set; } = string.Empty;

    [Required]
    public string CouldAnythingChangeChoice { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? WhatCouldChangeChoice { get; set; }

    [Required, MaxLength(60)]
    public string ElectionImportance { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? AdditionalComment { get; set; }
}
