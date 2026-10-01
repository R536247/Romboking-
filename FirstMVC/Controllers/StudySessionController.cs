using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;
using StudyRoomBooking.ViewModels;

namespace StudyRoomBooking.Controllers;

// CRUD for study sessions (bookings), including search and booking-conflict validation.
public class StudySessionController : Controller
{
    private readonly IStudySessionRepository _sessionRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<StudySessionController> _logger;

    public StudySessionController(IStudySessionRepository sessionRepository, IRoomRepository roomRepository,
        ILogger<StudySessionController> logger)
    {
        _sessionRepository = sessionRepository;
        _roomRepository = roomRepository;
        _logger = logger;
    }

    // GET: StudySession?search=ITPE3200
    public async Task<IActionResult> Index(string? search)
    {
        var sessions = await _sessionRepository.GetAll(search);
        if (sessions == null)
        {
            _logger.LogError("[StudySessionController] Session list could not be loaded");
            ViewData["Error"] = "The study sessions could not be loaded. Refresh the page to try again.";
            sessions = Enumerable.Empty<StudySession>();
        }

        return View(new StudySessionListViewModel { Sessions = sessions, Search = search });
    }

    // GET: StudySession Details 5
    public async Task<IActionResult> Details(int id)
    {
        var session = await _sessionRepository.GetById(id);
        if (session == null)
        {
            _logger.LogWarning("[StudySessionController] Session {Id} not found", id);
            return NotFound();
        }
        return View(session);
    }

    // GET: StudySession Create (optionally ?roomId=2 to preselect a room)
    public async Task<IActionResult> Create(int? roomId)
    {
        var tomorrow = DateTime.Today.AddDays(1);
        var session = new StudySession
        {
            StartTime = tomorrow.AddHours(10),
            EndTime = tomorrow.AddHours(12),
            RoomId = roomId ?? 0
        };

        await PopulateRoomsAsync(session.RoomId);
        return View(session);
    }

    // POST:  StudySession Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudySession session)
    {
        await ValidateBookingAsync(session);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[StudySessionController] Invalid input when creating a session: {Errors}", GetModelErrors());
            await PopulateRoomsAsync(session.RoomId);
            return View(session);
        }

        if (!await _sessionRepository.Create(session))
        {
            ModelState.AddModelError(string.Empty, "The session could not be saved. Try again in a moment.");
            await PopulateRoomsAsync(session.RoomId);
            return View(session);
        }

        TempData["Success"] = $"Booked: {session.CourseCode} – {session.Topic}.";
        return RedirectToAction(nameof(Details), new { id = session.StudySessionId });
    }

    // GET: StudySession Edit 5
    public async Task<IActionResult> Edit(int id)
    {
        var session = await _sessionRepository.GetById(id);
        if (session == null)
        {
            _logger.LogWarning("[StudySessionController] Session {Id} not found for editing", id);
            return NotFound();
        }

        await PopulateRoomsAsync(session.RoomId);
        return View(session);
    }

    // POST: StudySession Edit 5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudySession session)
    {
        if (id != session.StudySessionId)
        {
            _logger.LogWarning("[StudySessionController] Id mismatch on edit: route {RouteId}, form {FormId}",
                id, session.StudySessionId);
            return BadRequest();
        }

        await ValidateBookingAsync(session, excludeSessionId: session.StudySessionId);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[StudySessionController] Invalid input when editing session {Id}: {Errors}", id, GetModelErrors());
            await PopulateRoomsAsync(session.RoomId);
            return View(session);
        }

        if (!await _sessionRepository.Update(session))
        {
            ModelState.AddModelError(string.Empty, "The changes could not be saved. Try again in a moment.");
            await PopulateRoomsAsync(session.RoomId);
            return View(session);
        }

        TempData["Success"] = "Changes saved.";
        return RedirectToAction(nameof(Details), new { id = session.StudySessionId });
    }

    // GET: StudySession Delete 5 (confirmation page)
    public async Task<IActionResult> Delete(int id)
    {
        var session = await _sessionRepository.GetById(id);
        if (session == null)
        {
            _logger.LogWarning("[StudySessionController] Session {Id} not found for deletion", id);
            return NotFound();
        }
        return View(session);
    }

    // POST: StudySession Delete 5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!await _sessionRepository.Delete(id))
        {
            TempData["Error"] = "The session could not be deleted. It may already have been removed.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "The session was deleted and the room is free again.";
        return RedirectToAction(nameof(Index));
    }

    // Helpers 

    // Business rules that need the database: the room must exist, have enough
    // seats, and be free in the chosen time span.
    private async Task ValidateBookingAsync(StudySession session, int? excludeSessionId = null)
    {
        if (session.RoomId == 0)
            return; 

        var room = await _roomRepository.GetById(session.RoomId);
        if (room == null)
        {
            ModelState.AddModelError(nameof(session.RoomId), "The selected room does not exist.");
            return;
        }

        if (session.MaxParticipants > room.Capacity)
        {
            ModelState.AddModelError(nameof(session.MaxParticipants),
                $"{room.Name} has space for {room.Capacity} people. Lower the number or pick a bigger room.");
        }

        // Only check for conflicts when the time span itself is valid
        if (session.EndTime > session.StartTime &&
            await _sessionRepository.HasConflict(session.RoomId, session.StartTime, session.EndTime, excludeSessionId))
        {
            ModelState.AddModelError(nameof(session.StartTime),
                $"{room.Name} is already booked in this time span. Choose another time or room.");
        }
    }

    // Fills the room dropdown used by the Create and Edit forms.
    private async Task PopulateRoomsAsync(int? selectedRoomId = null)
    {
        var rooms = await _roomRepository.GetAll() ?? Enumerable.Empty<Room>();
        var items = rooms.Select(r => new
        {
            r.RoomId,
            Label = $"{r.Name} – {r.Building} ({r.Capacity} seats)"
        });
        ViewBag.Rooms = new SelectList(items, "RoomId", "Label", selectedRoomId);
    }

    // Collects validation errors as one string for logging.
    private string GetModelErrors() =>
        string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
}