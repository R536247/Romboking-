using Microsoft.AspNetCore.Mvc;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.Controllers;

// Lets students browse rooms and see each room's bookings
public class RoomController : Controller
{
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<RoomController> _logger;

    public RoomController(IRoomRepository roomRepository, ILogger<RoomController> logger)
    {
        _roomRepository = roomRepository;
        _logger = logger;
    }

    // GET: Room
    public async Task<IActionResult> Index()
    {
        var rooms = await _roomRepository.GetAll();
        if (rooms == null)
        {
            _logger.LogError("[RoomController] Room list could not be loaded");
            ViewData["Error"] = "The rooms could not be loaded. Refresh the page to try again.";
            rooms = Enumerable.Empty<Room>();
        }
        return View(rooms);
    }

    // GET: Room Details 5
    public async Task<IActionResult> Details(int id)
    {
        var room = await _roomRepository.GetById(id);
        if (room == null)
        {
            _logger.LogWarning("[RoomController] Room {RoomId} not found", id);
            return NotFound();
        }
        return View(room);
    }
}