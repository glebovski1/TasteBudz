namespace TasteBudz.Backend.Modules.Auth;

public sealed class CreatePasswordResetTokenRequest
{
    public string? UsernameOrEmail { get; init; }

    public Guid? PasswordResetRequestId { get; init; }
}
