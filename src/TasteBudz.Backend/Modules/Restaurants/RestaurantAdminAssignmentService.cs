using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Manages the global-Admin-controlled assignment of restaurant administrators.
/// </summary>
public sealed class RestaurantAdminAssignmentService(
    IRestaurantRepository restaurantRepository,
    IRestaurantOperationsRepository restaurantOperationsRepository,
    IAuthRepository authRepository,
    IClock clock,
    IPersistenceTransactionRunner? transactionRunner = null)
{
    private readonly IPersistenceTransactionRunner persistenceTransactionRunner = transactionRunner ?? NoOpPersistenceTransactionRunner.Instance;

    public async Task<IReadOnlyCollection<RestaurantAdminAssignmentDto>> ListAsync(
        CurrentUser currentUser,
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(currentUser);
        _ = await restaurantRepository.GetAsync(restaurantId, cancellationToken)
            ?? throw ApiException.NotFound("The requested restaurant could not be found.");

        var assignments = await restaurantOperationsRepository.ListAssignmentsForRestaurantAsync(restaurantId, cancellationToken);
        var results = new List<RestaurantAdminAssignmentDto>(assignments.Count);

        foreach (var assignment in assignments)
        {
            var account = await authRepository.GetByIdAsync(assignment.UserId, cancellationToken)
                ?? throw ApiException.NotFound("The assigned restaurant admin account could not be found.");
            results.Add(RestaurantOperationsMapper.ToAssignmentDto(assignment, account));
        }

        return results;
    }

    public async Task<RestaurantAdminAssignmentDto> GrantAsync(
        CurrentUser currentUser,
        Guid restaurantId,
        CreateRestaurantAdminAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(currentUser);
        _ = await restaurantRepository.GetAsync(restaurantId, cancellationToken)
            ?? throw ApiException.NotFound("The requested restaurant could not be found.");

        var username = string.IsNullOrWhiteSpace(request.Username)
            ? throw ApiException.BadRequest("username is required.")
            : request.Username.Trim();
        var account = await authRepository.FindByUsernameAsync(username, cancellationToken)
            ?? throw ApiException.NotFound("The requested user could not be found.");
        var now = clock.UtcNow;

        var assignment = await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                var existing = await restaurantOperationsRepository.GetAssignmentAsync(restaurantId, account.Id, cancellationToken);
                var activeAssignment = existing is not null && existing.RevokedAtUtc is null
                    ? existing
                    : new RestaurantAdminAssignment(restaurantId, account.Id, now, null);

                await restaurantOperationsRepository.SaveAssignmentAsync(activeAssignment, cancellationToken);

                if (!account.Roles.Contains(UserRole.RestaurantAdmin))
                {
                    var roles = account.Roles
                        .Append(UserRole.RestaurantAdmin)
                        .Distinct()
                        .OrderBy(role => role)
                        .ToArray();
                    await authRepository.UpdateAccountAsync(account with { Roles = roles, UpdatedAtUtc = now }, cancellationToken);
                }

                return activeAssignment;
            },
            cancellationToken);

        return RestaurantOperationsMapper.ToAssignmentDto(assignment, account);
    }

    public async Task RevokeAsync(
        CurrentUser currentUser,
        Guid restaurantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(currentUser);

        var assignment = await restaurantOperationsRepository.GetAssignmentAsync(restaurantId, userId, cancellationToken)
            ?? throw ApiException.NotFound("The requested restaurant admin assignment could not be found.");

        if (assignment.RevokedAtUtc is not null)
        {
            return;
        }

        var account = await authRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw ApiException.NotFound("The requested user could not be found.");
        var now = clock.UtcNow;

        await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await restaurantOperationsRepository.SaveAssignmentAsync(assignment with { RevokedAtUtc = now }, cancellationToken);
                var remainingAssignments = await restaurantOperationsRepository.ListAssignmentsForUserAsync(userId, cancellationToken);

                if (remainingAssignments.All(existing => existing.RestaurantId == restaurantId))
                {
                    var roles = account.Roles
                        .Where(role => role != UserRole.RestaurantAdmin)
                        .Distinct()
                        .OrderBy(role => role)
                        .ToArray();
                    await authRepository.UpdateAccountAsync(account with { Roles = roles, UpdatedAtUtc = now }, cancellationToken);
                }
            },
            cancellationToken);
    }

    private static void EnsureAdmin(CurrentUser currentUser)
    {
        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw ApiException.Forbidden("Only global admins can manage restaurant admin assignments.");
        }
    }
}
