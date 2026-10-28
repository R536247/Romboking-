using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyRoomBooking.Models;

// A booked study session in a room that other students can join.
// Form validation lives in StudySessionFormViewModel; business rules live in BookingService.
public class StudySession
{
    public int StudySessionId { get; set; }

    [Required, StringLength(9)]
    public string CourseCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Topic { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [Range(2, 50)]
    public int MaxParticipants { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public int OrganizerId { get; set; }
    public Student? Organizer { get; set; }

    public List<Participation> Participations { get; set; } = new();
    public List<Message> Messages { get; set; } = new();

    // Computed helpers (not stored). They require Participations to be loaded.
    [NotMapped]
    public bool IsFull => Participations.Count >= MaxParticipants;

    [NotMapped]
    public bool IsFinished => EndTime < DateTime.Now;
}
