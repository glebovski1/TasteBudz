namespace TasteBudz.Web.Mvc.Services;

public sealed record ImportResultDto(int Inserted, string Message, int Skipped = 0);
