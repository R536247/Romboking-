using System.ComponentModel.DataAnnotations;

namespace StudyRoomBooking.Models;

// A booked study session in a room that other students can join
public class StudySession : IValidatableObject
{
    public int StudySessionId { get; set; }

    [Required(ErrorMessage = "Enter a course code.")]
    [RegularExpression(@"^[A-Za-z]{3,5}\d{4}$", ErrorMessage = "Use a course code like DATA2500.")]
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

    [Required(ErrorMessage = "Choose a start time.")]
    [Display(Name = "Start time")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Choose an end time.")]
    [Display(Name = "End time")]
    public DateTime EndTime { get; set; }

    [Range(2, 50, ErrorMessage = "Max participants must be between 2 and 50.")]
    [Display(Name = "Max participants")]
    public int MaxParticipants { get; set; } = 6;

    [Required(ErrorMessage = "Enter your name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The name must be 2–100 characters.")]
    [Display(Name = "Your name")]
    public string OrganizerName { get; set; } = string.Empty;

    // Foreign key and navigation property to the booked room.
    // Range instead of Required, because an int is never null (0 means "not chosen").
    [Range(1, int.MaxValue, ErrorMessage = "Choose a room.")]
    [Display(Name = "Room")]
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    // Cross-field validation rules, checked on the server during model binding
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
            yield return new ValidationResult("The end time must be after the start time.", new[] { nameof(EndTime) });

        if (StartTime < DateTime.Now)
            yield return new ValidationResult("The start time cannot be in the past.", new[] { nameof(StartTime) });

        if ((EndTime - StartTime).TotalHours > 8)
            yield return new ValidationResult("A session can last at most 8 hours.", new[] { nameof(EndTime) });
    }
}