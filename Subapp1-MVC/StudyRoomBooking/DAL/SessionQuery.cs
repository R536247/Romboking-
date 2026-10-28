namespace StudyRoomBooking.DAL;

// Search and filter parameters for study sessions. All are optional and combined with AND.
public class SessionQuery
{
    public string? Search { get; set; }        // course code, subject or topic
    public string? Building { get; set; }
    public int? RoomId { get; set; }
    public DateTime? Date { get; set; }        // sessions starting on this day
    public bool OnlyAvailable { get; set; }    // hide full sessions
    public bool IncludeFinished { get; set; }  // also show sessions that have ended
}
