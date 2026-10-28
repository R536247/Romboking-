using System.ComponentModel.DataAnnotations;

namespace StudyRoomBooking.Models;

// A message a participant leaves in a study session.
public class Message
{
    public int MessageId { get; set; }

    [Required, StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? EditedAt { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int StudySessionId { get; set; }
    public StudySession? StudySession { get; set; }
}
