using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;
using StudyRoomBooking.ViewModels;

namespace StudyRoomBooking.Controllers;

// Rooms: everyone can browse; only admins can create, edit and delete.
public class RoomController : AppControllerBase
{
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<RoomController> _logger;

    public RoomController(IRoomRepository roomRepository, ILogger<RoomController> logger)
    {
        _roomRepository = roomRepository;
        _logger = logger;
    }

    // GET: /Room
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

    // GET: /Room/Details/5
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

    // GET: /Room/Create
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create()
    {
        return View(new RoomFormViewModel());
    }

    // POST: /Room/Create
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[RoomController] Invalid room input: {Errors}", ModelErrors());
            return View(model);
        }

        var room = model.ToEntity();
        if (!await _roomRepository.Create(room))
        {
            ModelState.AddModelError(string.Empty, "The room could not be saved. Try again in a moment.");
            return View(model);
        }

        TempData["Success"] = $"{room.Name} was added.";
        return RedirectToAction(nameof(Details), new { id = room.RoomId });
    }

    // GET: /Room/Edit/5
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _roomRepository.GetById(id);
        if (room == null)
        {
            _logger.LogWarning("[RoomController] Room {RoomId} not found for editing", id);
            return NotFound();
        }
        return View(RoomFormViewModel.FromEntity(room));
    }

    // POST: /Room/Edit/5
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoomFormViewModel model)
    {
        if (id != model.RoomId)
            return BadRequest();

        var existing = await _roomRepository.GetById(id);
        if (existing == null)
            return NotFound();

        // Domain rule: capacity cannot drop below what upcoming sessions in the room already allow
        var largestUpcoming = existing.StudySessions
            .Where(s => !s.IsFinished)
            .Select(s => s.MaxParticipants)
            .DefaultIfEmpty(0)
            .Max();
        if (model.Capacity < largestUpcoming)
        {
            ModelState.AddModelError(nameof(model.Capacity),
                $"An upcoming session in this room allows {largestUpcoming} people. Capacity cannot be lower.");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[RoomController] Invalid input when editing room {RoomId}: {Errors}", id, ModelErrors());
            return View(model);
        }

        if (!await _roomRepository.Update(model.ToEntity()))
        {
            ModelState.AddModelError(string.Empty, "The changes could not be saved. Try again in a moment.");
            return View(model);
        }

        TempData["Success"] = "Room saved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Room/Delete/5
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _roomRepository.GetById(id);
        if (room == null)
            return NotFound();

        return View(room);
    }

    // POST: /Room/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var room = await _roomRepository.GetById(id);
        if (room == null)
            return NotFound();

        if (room.StudySessions.Count > 0)
        {
            TempData["Error"] = $"{room.Name} has bookings and cannot be deleted. Delete its sessions first.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!await _roomRepository.Delete(id))
        {
            TempData["Error"] = "The room could not be deleted. Try again in a moment.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = $"{room.Name} was deleted.";
        return RedirectToAction(nameof(Index));
    }
}
