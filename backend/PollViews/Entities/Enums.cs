namespace PollViews.Entities;

public enum SurveyResponseStatus
{
    Draft = 1,
    Submitted = 2
}

public enum SurveyOffice
{
    Presidential = 1,
    Governorship = 2,
    Senate = 3,
    HouseOfRepresentatives = 4,
    HouseOfAssembly = 5
}

public enum SurveyReasonType
{
    Preferred = 1,
    NotPreferred = 2
}

public enum AdminReviewStatus
{
    Pending = 1,
    Approved = 2,
    Flagged = 3,
    Rejected = 4
}
