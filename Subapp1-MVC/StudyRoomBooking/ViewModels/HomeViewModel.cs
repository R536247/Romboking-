using StudyRoomBooking.Models;

namespace StudyRoomBooking.ViewModels;

// Data for the front page.
public class HomeViewModel
{
    public IEnumerable<StudySession> UpcomingSessions { get; set; } = Enumerable.Empty<StudySession>();
    public int RoomCount { get; set; }
}
