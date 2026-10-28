using System.ComponentModel.DataAnnotations;

namespace StudyRoomBooking.ViewModels;

// Form for editing the student profile.
public class ProfileEditViewModel
{
    [Required(ErrorMessage = "Enter your name.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "The name must be 2–60 characters.")]
    [Display(Name = "Name")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "The study program can be at most 100 characters.")]
    [Display(Name = "Study program")]
    public string? StudyProgram { get; set; }
}
