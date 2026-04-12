namespace TasteBudz.Web.Mvc.Controllers;

public sealed class ReportRequest
{
    public Guid UserId { get; init; }

    public string? Category { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string? Explanation { get; init; }
}
