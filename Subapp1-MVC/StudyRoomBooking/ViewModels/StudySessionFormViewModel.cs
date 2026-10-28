using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.ViewModels;

// Form for creating and editing a study session. Input validation lives here;
// rules that need the database (conflicts, capacity) are checked by BookingService.
public class StudySessionFormViewModel : IValidatableObject
{
    public int StudySessionId { get; set; }

    [Required(ErrorMessage = "Enter a course code.")]
    [RegularExpression(@"^[A-Za-z]{3,5}\d{4}$", ErrorMessage = "Use a course code like ITPE3200.")]
    [Display(Name = "Course code")]
    public string CourseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the subject.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The subject must be 2–100 characters.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter what you will work on.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "The topic must be 2–150 characters.")]
    public string Topic { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "The description can be at most 500 characters.")]
    public string? Description { get; set; }

    [Display(Name = "Start time")]
    public DateTime StartTime { get; set; }

    [Display(Name = "End time")]
    public DateTime EndTime { get; set; }

    [Range(2, 50, ErrorMessage = "Max participants must be between 2 and 50.")]
    [Display(Name = "Max participants")]
    public int MaxParticipants { get; set; } = 6;

    // Range instead of Required: an int is never null, 0 means "not chosen"
    [Range(1, int.MaxValue, ErrorMessage = "Choose a room.")]
    [Display(Name = "Room")]
    public int RoomId { get; set; }

    [BindNever, ValidateNever]
    public IEnumerable<SelectListItem> RoomOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    // Cross-field rules checked on the server during model binding.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
            yield return new ValidationResult("The end time must be after the start time.", new[] { nameof(EndTime) });

        if (StartTime < DateTime.Now)
            yield return new ValidationResult("The start time cannot be in the past.", new[] { nameof(StartTime) });

        if ((EndTime - StartTime).TotalHours > 8)
            yield return new ValidationResult("A session can last at most 8 hours.", new[] { nameof(EndTime) });
    }

    public StudySession ToEntity() => new()
    {
        StudySessionId = StudySessionId,
        CourseCode = CourseCode.Trim().ToUpperInvariant(),
        Subject = Subject.Trim(),
        Topic = Topic.Trim(),
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        StartTime = StartTime,
        EndTime = EndTime,
        MaxParticipants = MaxParticipants,
        RoomId = RoomId
    };

    public static StudySessionFormViewModel FromEntity(StudySession session) => new()
    {
        StudySessionId = session.StudySessionId,
        CourseCode = session.CourseCode,
        Subject = session.Subject,
        Topic = session.Topic,
        Description = session.Description,
        StartTime = session.StartTime,
        EndTime = session.EndTime,
        MaxParticipants = session.MaxParticipants,
        RoomId = session.RoomId
    };
}
