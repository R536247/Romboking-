using System.Collections.Generic;

namespace StudyRoomBooking.Models;

public class StudySessionListViewModel
{
    public IEnumerable<StudySession> Sessions { get; set; }
        = new List<StudySession>();

    public string? Search { get; set; }
}