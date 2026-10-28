using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Services;
using StudyRoomBooking.ViewModels;

namespace StudyRoomBooking.Controllers;

// Messages inside a study session. Create from the details page, edit and delete your own.
[Authorize]
public class MessageController : AppControllerBase
{
    private readonly IMessageRepository _messageRepository;
    private readonly IBookingService _bookingService;
    private readonly ICurrentStudentService _currentStudent;
    private readonly ILogger<MessageController> _logger;

    public MessageController(IMessageRepository messageRepository, IBookingService bookingService,
        ICurrentStudentService currentStudent, ILogger<MessageController> logger)
    {
        _messageRepository = messageRepository;
        _bookingService = bookingService;
        _currentStudent = currentStudent;
        _logger = logger;
    }

    // POST: /Message/Create (form on the session details page)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MessageFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[MessageController] Invalid message input: {Errors}", ModelErrors());
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                                ?? "The message could not be posted.";
            return BackToSession(model.StudySessionId);
        }

        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
            return Forbid();

        var result = await _bookingService.PostMessageAsync(model.StudySessionId, student.StudentId, model.Content);
        if (result.Status == ServiceStatus.NotFound)
            return NotFound();

        if (result.Succeeded)
            TempData["Success"] = "Message posted.";
        else
            TempData["Error"] = result.FirstError;

        return BackToSession(model.StudySessionId);
    }

    // GET: /Message/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var message = await _messageRepository.GetById(id);
        if (message == null)
        {
            _logger.LogWarning("[MessageController] Message {Id} not found for editing", id);
            return NotFound();
        }

        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null || message.StudentId != student.StudentId)
            return Forbid();

        return View(new MessageFormViewModel
        {
            MessageId = message.MessageId,
            StudySessionId = message.StudySessionId,
            Content = message.Content
        });
    }

    // POST: /Message/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MessageFormViewModel model)
    {
        if (id != model.MessageId)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[MessageController] Invalid input when editing message {Id}: {Errors}", id, ModelErrors());
            return View(model);
        }

        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
            return Forbid();

        var result = await _bookingService.UpdateMessageAsync(id, student.StudentId, model.Content);
        var httpError = HttpErrorFor(result);
        if (httpError != null)
            return httpError;

        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            return View(model);
        }

        TempData["Success"] = "Message updated.";
        return BackToSession(model.StudySessionId);
    }

    // POST: /Message/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var message = await _messageRepository.GetById(id);
        if (message == null)
            return NotFound();

        var student = await _currentStudent.GetCurrentStudentAsync();
        if (student == null)
            return Forbid();

        var result = await _bookingService.DeleteMessageAsync(id, student.StudentId, _currentStudent.IsAdmin);
        var httpError = HttpErrorFor(result);
        if (httpError != null)
            return httpError;

        if (result.Succeeded)
            TempData["Success"] = "Message deleted.";
        else
            TempData["Error"] = result.FirstError;

        return BackToSession(message.StudySessionId);
    }

    // Redirects to the messages section of the session page.
    private IActionResult BackToSession(int sessionId) =>
        RedirectToAction("Details", "StudySession", new { id = sessionId }, "messages");
}
