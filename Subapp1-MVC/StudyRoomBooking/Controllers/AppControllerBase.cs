using Microsoft.AspNetCore.Mvc;
using StudyRoomBooking.Services;

namespace StudyRoomBooking.Controllers;

// Shared helpers for turning ServiceResult outcomes into MVC responses.
public abstract class AppControllerBase : Controller
{
    // Returns 404/403 for NotFound/Forbidden results, otherwise null.
    protected IActionResult? HttpErrorFor(ServiceResult result) => result.Status switch
    {
        ServiceStatus.NotFound => NotFound(),
        ServiceStatus.Forbidden => Forbid(),
        _ => null
    };

    // Copies service errors into ModelState so they show up in the form.
    protected void AddServiceErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Field, error.Message);
        }
    }

    // Returns all validation errors as one string, for logging.
    protected string ModelErrors() =>
        string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
}
