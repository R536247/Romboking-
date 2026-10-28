using System.ComponentModel.DataAnnotations;

namespace StudyRoomBooking.ViewModels;

// Registration form. Creates both the login and the student profile.
public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your name.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "The name must be 2–60 characters.")]
    [Display(Name = "Name")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "The study program can be at most 100 characters.")]
    [Display(Name = "Study program")]
    public string? StudyProgram { get; set; }

    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repeat the password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Repeat password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
