using System.ComponentModel.DataAnnotations;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.ViewModels;

// Form for creating and editing rooms (admin only).
public class RoomFormViewModel
{
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Enter the room name.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "The name must be 2–50 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the building.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The building must be 2–100 characters.")]
    public string Building { get; set; } = string.Empty;

    [Range(1, 50, ErrorMessage = "Capacity must be between 1 and 50.")]
    public int Capacity { get; set; } = 6;

    [Display(Name = "Has a screen")]
    public bool HasScreen { get; set; }

    public Room ToEntity() => new()
    {
        RoomId = RoomId,
        Name = Name.Trim(),
        Building = Building.Trim(),
        Capacity = Capacity,
        HasScreen = HasScreen
    };

    public static RoomFormViewModel FromEntity(Room room) => new()
    {
        RoomId = room.RoomId,
        Name = room.Name,
        Building = room.Building,
        Capacity = room.Capacity,
        HasScreen = room.HasScreen
    };
}
