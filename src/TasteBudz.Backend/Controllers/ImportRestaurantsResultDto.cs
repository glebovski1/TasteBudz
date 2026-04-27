namespace TasteBudz.Backend.Controllers;

public sealed record ImportRestaurantsResultDto(int Inserted, string Message, int Skipped = 0);
