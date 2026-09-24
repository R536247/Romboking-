using Microsoft.AspNetCore.Mvc;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.Controllers;

public class RoomController : Controller
{
    // added test/prototype data
    private static readonly List<Room> _rooms = new()
    {
        new Room
        {
            RoomId = 1,
            Name = "Study Room A",
            Building = "Pilestredet 35",
            Capacity = 6,
            HasScreen = true
        },

        new Room
        {
                        RoomId = 2,
            Name = "Study Room B",
            Building = "Pilestredet 52",
            Capacity = 4,
            HasScreen = false
        }
    };
    public RoomController()
    {

    }

    // Pending Views/Rooms/Index.cshtml
    public IActionResult Index()
    {
        return View(_rooms);
    }

    public IActionResult Details(int id)
    {
        var room = _rooms.FirstOrDefault(n => n.RoomId == id);
        if (room == null)
        {
            return NotFound();
        }
        return View(room);
    }

    public IActionResult Create(Room room)
    {
        if (!ModelState.IsValid)
        {
            return View(room);
        }
        // tmp generator for RoomId, checks if there are rooms in list, 
        // increment Id if so. This can later be handled bt DB with Primary Key etc
        room.RoomId = _rooms.Count == 0 ? 1 : _rooms.Max(n => n.RoomId) + 1;
        _rooms.Add(room);

        return RedirectToAction(nameof(Index));
    }
}