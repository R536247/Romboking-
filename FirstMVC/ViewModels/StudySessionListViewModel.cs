using StudyRoomBooking.Models;

namespace StudyRoomBooking.ViewModels;

// Data for the session list: the sessions found and the search text used.
public class StudySessionListViewModel
{
    public IEnumerable<StudySession> Sessions { get; set; } = Enumerable.Empty<StudySession>();
    public string? Search { get; set; }
}