namespace PollViews.DTOs;

public sealed record GeographyOption(string Code, string Name, string PortalId);

public sealed record PollingUnitOption(string Code, string Name, string Location, string FullCode, string PortalId);

public sealed record ElectionCandidateOption(string Value, string Label);
