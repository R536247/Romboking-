using Microsoft.AspNetCore.Mvc;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.Controllers;

public class RoomController : Controller
{
    // added test/prototype data
    private List<Room> _rooms = new()
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
        return View();
    }
}