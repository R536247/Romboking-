using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;
using StudyRoomBooking.ViewModels;

namespace StudyRoomBooking.Controllers;

// Front page and the global error  status code pages.
public class HomeController : Controller
{
    private readonly IStudySessionRepository _sessionRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IStudySessionRepository sessionRepository, IRoomRepository roomRepository,
        ILogger<HomeController> logger)
    {
        _sessionRepository = sessionRepository;
        _roomRepository = roomRepository;
        _logger = logger;
    }

    // GET: 
    public async Task<IActionResult> Index()
    {
        var upcoming = await _sessionRepository.GetUpcoming(4);
        var rooms = await _roomRepository.GetAll();

        if (upcoming == null || rooms == null)
        {
            _logger.LogError("[HomeController] Could not load data for the front page");
            ViewData["Error"] = "Some content could not be loaded. Refresh the page to try again.";
        }

        var viewModel = new HomeViewModel
        {
            UpcomingSessions = upcoming ?? Enumerable.Empty<StudySession>(),
            RoomCount = rooms?.Count() ?? 0
        };
        return View(viewModel);
    }

    // Shown by UseExceptionHandler when an unhandled exception occurs
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (feature != null)
        {
            _logger.LogError(feature.Error, "[HomeController] Unhandled exception on {Path}", feature.Path);
        }

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Shown by UseStatusCodePagesWithReExecute for 404 and other status codes
    public IActionResult HttpStatus(int code)
    {
        _logger.LogWarning("[HomeController] Status code {Code} returned", code);
        return View(code);
    }
}