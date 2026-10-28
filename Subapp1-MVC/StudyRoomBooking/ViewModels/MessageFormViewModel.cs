using System.ComponentModel.DataAnnotations;

namespace StudyRoomBooking.ViewModels;

// Form for writing or editing a message in a session.
public class MessageFormViewModel
{
    public int MessageId { get; set; }
    public int StudySessionId { get; set; }

    [Required(ErrorMessage = "Write a message before sending.")]
    [StringLength(1000, ErrorMessage = "A message can be at most 1000 characters.")]
    [Display(Name = "Message")]
    public string Content { get; set; } = string.Empty;
}
