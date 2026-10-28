using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.ViewModels;

// Filter values from the query string, plus the results and dropdown options for the session list.
public class SessionFilterViewModel
{
    // Filter input (bound from the query string) 
    public string? Search { get; set; }
    public string? Building { get; set; }

    [Display(Name = "Room")]
    public int? RoomId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }

    [Display(Name = "Only sessions with free spots")]
    public bool OnlyAvailable { get; set; }

    [Display(Name = "Include finished sessions")]
    public bool IncludeFinished { get; set; }

    // Output for the view (never bound from the request)
    [BindNever, ValidateNever]
    public IEnumerable<StudySession> Sessions { get; set; } = Enumerable.Empty<StudySession>();

    [BindNever, ValidateNever]
    public IEnumerable<SelectListItem> BuildingOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    [BindNever, ValidateNever]
    public IEnumerable<SelectListItem> RoomOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Building) ||
        RoomId.HasValue || Date.HasValue || OnlyAvailable || IncludeFinished;

    public SessionQuery ToQuery() => new()
    {
        Search = Search,
        Building = Building,
        RoomId = RoomId,
        Date = Date,
        OnlyAvailable = OnlyAvailable,
        IncludeFinished = IncludeFinished
    };
}
