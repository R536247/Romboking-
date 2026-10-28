using System.ComponentModel.DataAnnotations;

namespace StudyRoomBooking.Models;

// A physical study room on campus that students can book.
public class Room
{
    public int RoomId { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Building { get; set; } = string.Empty;

    [Range(1, 50)]
    public int Capacity { get; set; }

    public bool HasScreen { get; set; }

    // All sessions booked in this room (1:N)
    public List<StudySession> StudySessions { get; set; } = new();
}
