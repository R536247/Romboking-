using System.ComponentModel.DataAnnotations;

namespace StudyRoomBooking.Models;

// The domain profile of a logged-in user. Identity handles login (IdentityUser);
// this entity holds what the booking domain needs and is linked to the user by UserId.
public class Student
{
    public int StudentId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? StudyProgram { get; set; }

    // Sessions this student has booked as organizer (1:N)
    public List<StudySession> OrganizedSessions { get; set; } = new();

    // Sessions this student takes part in (N:M through Participation)
    public List<Participation> Participations { get; set; } = new();

    public List<Message> Messages { get; set; } = new();
}
