using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;
using System.Globalization;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class EventFeedbackFormViewModel
{
    public Guid EventId { get; set; }

    [Required(ErrorMessage = "Rating is required.")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int? Rating { get; set; }

    [Required(ErrorMessage = "Feedback is required.")]
    [MaxLength(1000, ErrorMessage = "Feedback cannot exceed 1000 characters.")]
    public string Text { get; set; } = string.Empty;

    public UpsertEventFeedbackRequest ToRequest() => new()
    {
        Rating = Rating,
        Text = Text,
    };
}
