using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class ChatInboxItemViewModel
{
    public Guid ScopeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ScopeLabel { get; init; } = string.Empty; // "Event" or "Group"
    public string ChatUrl { get; init; } = string.Empty;
}
