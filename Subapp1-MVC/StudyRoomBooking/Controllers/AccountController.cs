using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;
using StudyRoomBooking.ViewModels;

namespace StudyRoomBooking.Controllers;

// Registration, login and logout with ASP.NET Core Identity.
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager,
        IStudentRepository studentRepository, ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _studentRepository = studentRepository;
        _logger = logger;
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("[AccountController] {Email} logged in", model.Email);
                // Only redirect to local URLs to prevent open-redirect attacks
                return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "Home");
            }

            _logger.LogWarning("[AccountController] Failed login for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Wrong email or password.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AccountController] Login failed for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Login is not available right now. Try again in a moment.");
        }

        return View(model);
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                _logger.LogWarning("[AccountController] Registration rejected for {Email}", model.Email);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, Roles.Student);

            // Every login gets a student profile used by the booking domain
            var student = new Student
            {
                UserId = user.Id,
                DisplayName = model.DisplayName.Trim(),
                StudyProgram = string.IsNullOrWhiteSpace(model.StudyProgram) ? null : model.StudyProgram.Trim()
            };

            if (!await _studentRepository.Create(student))
            {
                // Roll back the login so we never have a user without a profile
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "The account could not be created. Try again in a moment.");
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("[AccountController] Registered new student {Email}", model.Email);
            TempData["Success"] = $"Welcome, {student.DisplayName}! You can now book rooms and join study groups.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AccountController] Registration failed for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "The account could not be created. Try again in a moment.");
            return View(model);
        }
    }

    // POST: /Account/Logout
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("[AccountController] User logged out");
        TempData["Success"] = "You are logged out.";
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        _logger.LogWarning("[AccountController] Access denied for {User}", User.Identity?.Name ?? "anonymous");
        return View();
    }
}
