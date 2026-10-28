using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;
using StudyRoomBooking.Services;
using StudyRoomBooking.ViewModels;

namespace StudyRoomBooking.Controllers;

// "My page": the logged-in student's profile and sessions.
[Authorize]
public class ProfileController : AppControllerBase
{
    private readonly ICurrentStudentService _currentStudent;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudySessionRepository _sessionRepository;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(ICurrentStudentService currentStudent, IStudentRepository studentRepository,
        IStudySessionRepository sessionRepository, ILogger<ProfileController> logger)
    {
        _currentStudent = currentStudent;
        _studentRepository = studentRepository;
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    // GET: /Profile
    public async Task<IActionResult> Index()
    {
        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
        {
            _logger.LogWarning("[ProfileController] Logged-in user {User} has no student profile", User.Identity?.Name);
            return NotFound();
        }

        var sessions = await _sessionRepository.GetForStudent(student.StudentId);
        if (sessions == null)
        {
            ViewData["Error"] = "Your sessions could not be loaded. Refresh the page to try again.";
            sessions = Enumerable.Empty<StudySession>();
        }

        return View(new ProfileViewModel
        {
            Student = student,
            Email = User.Identity?.Name ?? string.Empty,
            OrganizedSessions = sessions.Where(s => s.OrganizerId == student.StudentId).ToList(),
            JoinedSessions = sessions.Where(s => s.OrganizerId != student.StudentId).ToList()
        });
    }

    // GET: /Profile/Edit
    public async Task<IActionResult> Edit()
    {
        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
            return NotFound();

        return View(new ProfileEditViewModel { DisplayName = student.DisplayName, StudyProgram = student.StudyProgram });
    }

    // POST: /Profile/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[ProfileController] Invalid profile input: {Errors}", ModelErrors());
            return View(model);
        }

        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
            return NotFound();

        student.DisplayName = model.DisplayName.Trim();
        student.StudyProgram = string.IsNullOrWhiteSpace(model.StudyProgram) ? null : model.StudyProgram.Trim();

        if (!await _studentRepository.Update(student))
        {
            ModelState.AddModelError(string.Empty, "Your profile could not be saved. Try again in a moment.");
            return View(model);
        }

        TempData["Success"] = "Profile saved.";
        return RedirectToAction(nameof(Index));
    }
}
