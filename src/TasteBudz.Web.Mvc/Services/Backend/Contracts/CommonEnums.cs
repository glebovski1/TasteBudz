namespace TasteBudz.Web.Mvc.Services.Backend.Contracts;

public enum UserRole
{
    User,
    Moderator,
    Admin,
}

public enum SocialGoal
{
    Friends,
    Dating,
    Networking,
}

public enum SpiceTolerance
{
    Mild,
    Medium,
    Hot,
}

public enum EventStatus
{
    Open,
    Full,
    Confirmed,
    Cancelled,
    Completed,
}

public enum GroupVisibility
{
    Public,
    Private,
}
