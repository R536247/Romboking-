namespace StudyRoomBooking.Models;

// Join entity for the many-to-many relationship between Student and StudySession:
// a student can join many sessions, and a session has many participants.
// The composite key (StudentId, StudySessionId) is configured in AppDbContext.
public class Participation
{
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int StudySessionId { get; set; }
    public StudySession? StudySession { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.Now;
}
