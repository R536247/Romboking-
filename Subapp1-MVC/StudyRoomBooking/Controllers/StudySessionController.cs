using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;
using StudyRoomBooking.Services;
using StudyRoomBooking.ViewModels;

namespace StudyRoomBooking.Controllers;

// Study sessions: browse and filter (everyone), book/edit/delete (organizer or admin), join and leave (students).
// Business rules are enforced by IBookingService.
public class StudySessionController : AppControllerBase
{
    private readonly IStudySessionRepository _sessionRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingService _bookingService;
    private readonly ICurrentStudentService _currentStudent;
    private readonly ILogger<StudySessionController> _logger;

    public StudySessionController(IStudySessionRepository sessionRepository, IRoomRepository roomRepository,
        IBookingService bookingService, ICurrentStudentService currentStudent, ILogger<StudySessionController> logger)
    {
        _sessionRepository = sessionRepository;
        _roomRepository = roomRepository;
        _bookingService = bookingService;
        _currentStudent = currentStudent;
        _logger = logger;
    }

    // GET: /StudySession?Search=ITPE&Building=...&Date=...&OnlyAvailable=true
    public async Task<IActionResult> Index(SessionFilterViewModel filter)
    {
        var sessions = await _sessionRepository.GetFiltered(filter.ToQuery());
        if (sessions == null)
        {
            _logger.LogError("[StudySessionController] Session list could not be loaded");
            ViewData["Error"] = "The study sessions could not be loaded. Refresh the page to try again.";
            sessions = Enumerable.Empty<StudySession>();
        }

        filter.Sessions = sessions;
        await LoadFilterOptionsAsync(filter);
        return View(filter);
    }

    // GET: /StudySession/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var session = await _sessionRepository.GetById(id);
        if (session == null)
        {
            _logger.LogWarning("[StudySessionController] Session {Id} not found", id);
            return NotFound();
        }

        var student = await _currentStudent.GetCurrentStudentAsync();
        var isAdmin = _currentStudent.IsAdmin;
        var studentId = student?.StudentId;

        var viewModel = new StudySessionDetailsViewModel
        {
            Session = session,
            CurrentStudentId = studentId,
            IsAuthenticated = student != null,
            IsAdmin = isAdmin,
            IsParticipant = studentId.HasValue && session.Participations.Any(p => p.StudentId == studentId.Value),
            IsOrganizer = studentId.HasValue && session.OrganizerId == studentId.Value,
            CanManage = _bookingService.CanManageSession(session, studentId, isAdmin),
            NewMessage = new MessageFormViewModel { StudySessionId = id }
        };
        return View(viewModel);
    }

    // GET: /StudySession/Create (optionally ?roomId=2 to preselect a room)
    [Authorize]
    public async Task<IActionResult> Create(int? roomId)
    {
        var tomorrow = DateTime.Today.AddDays(1);
        var model = new StudySessionFormViewModel
        {
            StartTime = tomorrow.AddHours(10),
            EndTime = tomorrow.AddHours(12),
            RoomId = roomId ?? 0
        };

        await LoadRoomOptionsAsync(model);
        return View(model);
    }

    // POST: /StudySession/Create
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudySessionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[StudySessionController] Invalid input when creating a session: {Errors}", ModelErrors());
            await LoadRoomOptionsAsync(model);
            return View(model);
        }

        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
            return Forbid();

        var session = model.ToEntity();
        var result = await _bookingService.CreateSessionAsync(session, student.StudentId);
        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            await LoadRoomOptionsAsync(model);
            return View(model);
        }

        TempData["Success"] = $"Booked: {session.CourseCode} – {session.Topic}. You are on the participant list.";
        return RedirectToAction(nameof(Details), new { id = session.StudySessionId });
    }

    // GET: /StudySession/Edit/5
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var session = await _sessionRepository.GetById(id);
        if (session == null)
        {
            _logger.LogWarning("[StudySessionController] Session {Id} not found for editing", id);
            return NotFound();
        }

        var student = await _currentStudent.GetCurrentStudentAsync();
        if (!_bookingService.CanManageSession(session, student?.StudentId, _currentStudent.IsAdmin))
        {
            _logger.LogWarning("[StudySessionController] Student {StudentId} denied editing session {Id}", student?.StudentId, id);
            return Forbid();
        }

        var model = StudySessionFormViewModel.FromEntity(session);
        await LoadRoomOptionsAsync(model);
        return View(model);
    }

    // POST: /StudySession/Edit/5
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudySessionFormViewModel model)
    {
        if (id != model.StudySessionId)
        {
            _logger.LogWarning("[StudySessionController] Id mismatch on edit: route {RouteId}, form {FormId}", id, model.StudySessionId);
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[StudySessionController] Invalid input when editing session {Id}: {Errors}", id, ModelErrors());
            await LoadRoomOptionsAsync(model);
            return View(model);
        }

        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
            return Forbid();

        var result = await _bookingService.UpdateSessionAsync(model.ToEntity(), student.StudentId, _currentStudent.IsAdmin);
        var httpError = HttpErrorFor(result);
        if (httpError != null)
            return httpError;

        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            await LoadRoomOptionsAsync(model);
            return View(model);
        }

        TempData["Success"] = "Changes saved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /StudySession/Delete/5 (confirmation page)
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var session = await _sessionRepository.GetById(id);
        if (session == null)
            return NotFound();

        var student = await _currentStudent.GetCurrentStudentAsync();
        if (!_bookingService.CanManageSession(session, student?.StudentId, _currentStudent.IsAdmin))
            return Forbid();

        return View(session);
    }

    // POST: /StudySession/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
            return Forbid();

        var result = await _bookingService.DeleteSessionAsync(id, student.StudentId, _currentStudent.IsAdmin);
        var httpError = HttpErrorFor(result);
        if (httpError != null)
            return httpError;

        if (!result.Succeeded)
        {
            TempData["Error"] = result.FirstError;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "The session was deleted and the room is free again.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /StudySession/Join/5
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id)
    {
        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
            return Forbid();

        var result = await _bookingService.JoinSessionAsync(id, student.StudentId);
        var httpError = HttpErrorFor(result);
        if (httpError != null)
            return httpError;

        if (result.Succeeded)
            TempData["Success"] = "You joined the session. See you there!";
        else
            TempData["Error"] = result.FirstError;

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /StudySession/Leave/5
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
            return Forbid();

        var result = await _bookingService.LeaveSessionAsync(id, student.StudentId);
        var httpError = HttpErrorFor(result);
        if (httpError != null)
            return httpError;

        if (result.Succeeded)
            TempData["Success"] = "You left the session.";
        else
            TempData["Error"] = result.FirstError;

        return RedirectToAction(nameof(Details), new { id });
    }

    // Helper

    // Fills the room dropdown used by the Create and Edit forms.
    private async Task LoadRoomOptionsAsync(StudySessionFormViewModel model)
    {
        var rooms = await _roomRepository.GetAll() ?? Enumerable.Empty<Room>();
        model.RoomOptions = rooms.Select(r => new SelectListItem(
            $"{r.Name} – {r.Building} ({r.Capacity} seats)",
            r.RoomId.ToString(),
            r.RoomId == model.RoomId)).ToList();
    }

    // Fills the building and room dropdowns of the filter panel.
    private async Task LoadFilterOptionsAsync(SessionFilterViewModel filter)
    {
        var rooms = (await _roomRepository.GetAll() ?? Enumerable.Empty<Room>()).ToList();

        filter.BuildingOptions = rooms
            .Select(r => r.Building)
            .Distinct()
            .OrderBy(b => b)
            .Select(b => new SelectListItem(b, b, b == filter.Building))
            .ToList();

        filter.RoomOptions = rooms
            .Select(r => new SelectListItem(r.Name, r.RoomId.ToString(), r.RoomId == filter.RoomId))
            .ToList();
    }
}
