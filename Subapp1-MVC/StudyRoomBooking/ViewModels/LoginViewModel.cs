using System.ComponentModel.DataAnnotations;

namespace StudyRoomBooking.ViewModels;

// Login form.
public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me logged in")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
