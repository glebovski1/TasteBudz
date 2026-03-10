// Business rules for recurring and one-off availability windows.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;

namespace TasteBudz.Backend.Modules.Profiles;

/// <summary>
/// Validates and stores availability windows for the current user.
/// </summary>
public sealed class AvailabilityService(IProfileRepository profileRepository, IClock clock)
{
    /// <summary>
    /// Returns the caller's recurring weekly windows exactly as they are currently stored.
    /// </summary>
    public async Task<IReadOnlyCollection<RecurringAvailabilityWindowDto>> ListRecurringAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await profileRepository.ListRecurringAvailabilityAsync(userId, cancellationToken))
            .Select(window => new RecurringAvailabilityWindowDto(window.Id, window.DayOfWeek, window.StartTime, window.EndTime, window.Label))
            .ToArray();

    /// <summary>
    /// Creates a recurring availability window or updates an existing one for the same user.
    /// </summary>
    public async Task<RecurringAvailabilityWindowDto> UpsertRecurringAsync(Guid userId, Guid? windowId, UpsertRecurringAvailabilityWindowRequest request, CancellationToken cancellationToken = default)
    {
        // Read the required time bounds once so the validation and persistence paths use the same values.
        var startTime = request.StartTime ?? throw ApiException.BadRequest("startTime is required.");
        var endTime = request.EndTime ?? throw ApiException.BadRequest("endTime is required.");

        if (startTime >= endTime)
        {
            throw ApiException.BadRequest("Recurring availability startTime must be earlier than endTime.");
        }

        // PATCH must target an existing row; POST passes a null id and therefore skips this lookup.
        var existing = windowId.HasValue
            ? await profileRepository.GetRecurringAvailabilityAsync(userId, windowId.Value, cancellationToken)
            : null;

        if (windowId.HasValue && existing is null)
        {
            throw ApiException.NotFound("The requested recurring availability window could not be found.");
        }

        // Keep the original identifier and creation time when editing so history remains stable.
        var now = clock.UtcNow;
        var window = new RecurringAvailabilityWindow(
            existing?.Id ?? windowId ?? Guid.NewGuid(),
            userId,
            request.DayOfWeek ?? throw ApiException.BadRequest("dayOfWeek is required."),
            startTime,
            endTime,
            string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim(),
            existing?.CreatedAtUtc ?? now,
            now);

        await profileRepository.SaveRecurringAvailabilityAsync(window, cancellationToken);
        return new RecurringAvailabilityWindowDto(window.Id, window.DayOfWeek, window.StartTime, window.EndTime, window.Label);
    }

    /// <summary>
    /// Deletes one recurring availability window owned by the supplied user.
    /// </summary>
    public Task DeleteRecurringAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default) =>
        profileRepository.DeleteRecurringAvailabilityAsync(userId, windowId, cancellationToken);

    /// <summary>
    /// Returns the caller's one-off date/time windows exactly as they are currently stored.
    /// </summary>
    public async Task<IReadOnlyCollection<OneOffAvailabilityWindowDto>> ListOneOffAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await profileRepository.ListOneOffAvailabilityAsync(userId, cancellationToken))
            .Select(window => new OneOffAvailabilityWindowDto(window.Id, window.StartsAtUtc, window.EndsAtUtc, window.Label))
            .ToArray();

    /// <summary>
    /// Creates a one-off availability window or updates an existing one for the same user.
    /// </summary>
    public async Task<OneOffAvailabilityWindowDto> UpsertOneOffAsync(Guid userId, Guid? windowId, UpsertOneOffAvailabilityWindowRequest request, CancellationToken cancellationToken = default)
    {
        // One-off windows use explicit UTC timestamps instead of day/time pairs.
        var startsAt = request.StartsAtUtc ?? throw ApiException.BadRequest("startsAtUtc is required.");
        var endsAt = request.EndsAtUtc ?? throw ApiException.BadRequest("endsAtUtc is required.");

        if (startsAt >= endsAt)
        {
            throw ApiException.BadRequest("One-off availability startsAtUtc must be earlier than endsAtUtc.");
        }

        var existing = windowId.HasValue
            ? await profileRepository.GetOneOffAvailabilityAsync(userId, windowId.Value, cancellationToken)
            : null;

        if (windowId.HasValue && existing is null)
        {
            throw ApiException.NotFound("The requested one-off availability window could not be found.");
        }

        // Preserve the original row identity during edits and stamp the modification time on every write.
        var now = clock.UtcNow;
        var window = new OneOffAvailabilityWindow(
            existing?.Id ?? windowId ?? Guid.NewGuid(),
            userId,
            startsAt,
            endsAt,
            string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim(),
            existing?.CreatedAtUtc ?? now,
            now);

        await profileRepository.SaveOneOffAvailabilityAsync(window, cancellationToken);
        return new OneOffAvailabilityWindowDto(window.Id, window.StartsAtUtc, window.EndsAtUtc, window.Label);
    }

    /// <summary>
    /// Deletes one one-off availability window owned by the supplied user.
    /// </summary>
    public Task DeleteOneOffAsync(Guid userId, Guid windowId, CancellationToken cancellationToken = default) =>
        profileRepository.DeleteOneOffAvailabilityAsync(userId, windowId, cancellationToken);
}
