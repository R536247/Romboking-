using StudyRoomBooking.Models;

namespace StudyRoomBooking.ViewModels;

// Data for the front page: the next sessions and a room count.
public class HomeViewModel
{
    public IEnumerable<StudySession> UpcomingSessions { get; set; } = Enumerable.Empty<StudySession>();
    public int RoomCount { get; set; }
}