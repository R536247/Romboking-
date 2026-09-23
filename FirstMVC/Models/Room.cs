using System.ComponentModel.DataAnnotations;

namespace StudyRoomBooking.Models;

// A physical study room on campus that students can book
public class Room
{
    public int RoomId { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Building { get; set; } = string.Empty;

    [Range(1, 50)]
    public int Capacity { get; set; }

    [Display(Name = "Screen")]
    public bool HasScreen { get; set; }

    // Navigation property: all sessions booked in this room
    public List<StudySession> StudySessions { get; set; } = new();
}