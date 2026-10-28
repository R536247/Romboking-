using StudyRoomBooking.Models;

namespace StudyRoomBooking.ViewModels;

//The "My page" overview: profile details and the student's sessions.
public class ProfileViewModel
{
    public Student Student { get; set; } = new();
    public string Email { get; set; } = string.Empty;
    public IEnumerable<StudySession> OrganizedSessions { get; set; } = Enumerable.Empty<StudySession>();
    public IEnumerable<StudySession> JoinedSessions { get; set; } = Enumerable.Empty<StudySession>();
}
