namespace TasteBudz.Backend.Modules.Profiles;

public sealed record OnboardingStatusDto(bool IsComplete, IReadOnlyCollection<string> MissingRequiredFields);
