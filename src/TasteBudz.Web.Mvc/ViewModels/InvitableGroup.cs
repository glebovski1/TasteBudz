using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class InvitableGroup
{
    public Guid GroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<GroupMemberDto> Members { get; init; } = [];
}
