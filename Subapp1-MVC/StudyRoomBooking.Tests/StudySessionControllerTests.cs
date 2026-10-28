using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudyRoomBooking.Controllers;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;
using StudyRoomBooking.Services;
using StudyRoomBooking.ViewModels;

namespace StudyRoomBooking.Tests;

/// <summary>Tests of StudySessionController: correct views, redirects and status codes.
/// The repositories, booking service and current-student lookup are mocked.</summary>
public class StudySessionControllerTests
{
    private readonly Mock<IStudySessionRepository> _sessions = new();
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IBookingService> _booking = new();
    private readonly Mock<ICurrentStudentService> _currentStudent = new();

    private StudySessionController CreateController()
    {
        var controller = new StudySessionController(_sessions.Object, _rooms.Object, _booking.Object,
            _currentStudent.Object, NullLogger<StudySessionController>.Instance);

        // Controllers need an HttpContext and TempData to run outside the web server
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    private void LogInAs(int studentId) =>
        _currentStudent.Setup(c => c.GetCurrentStudentAsync()).ReturnsAsync(TestData.Student(studentId));

    private static StudySessionFormViewModel ValidForm() => StudySessionFormViewModel.FromEntity(TestData.Session(id: 0));

    // ---------- Positive ----------

    [Fact]
    public async Task Index_ReturnsViewWithFilteredSessions()
    {
        _sessions.Setup(s => s.GetFiltered(It.IsAny<SessionQuery>()))
            .ReturnsAsync(new List<StudySession> { TestData.Session(1), TestData.Session(2) });
        var controller = CreateController();

        var result = await controller.Index(new SessionFilterViewModel { Search = "ITPE", OnlyAvailable = true });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<SessionFilterViewModel>(view.Model);
        Assert.Equal(2, model.Sessions.Count());
        _sessions.Verify(s => s.GetFiltered(It.Is<SessionQuery>(q => q.Search == "ITPE" && q.OnlyAvailable)), Times.Once);
    }

    [Fact]
    public async Task Details_ExistingSession_ReturnsViewModelWithParticipantFlags()
    {
        _sessions.Setup(s => s.GetById(1)).ReturnsAsync(TestData.Session(1, 4, TestData.OtherStudentId));
        LogInAs(TestData.OtherStudentId);
        var controller = CreateController();

        var result = await controller.Details(1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StudySessionDetailsViewModel>(view.Model);
        Assert.True(model.IsParticipant);
        Assert.False(model.IsOrganizer);
    }

    [Fact]
    public async Task Create_ValidModel_RedirectsToDetails()
    {
        LogInAs(TestData.OrganizerId);
        _booking.Setup(b => b.CreateSessionAsync(It.IsAny<StudySession>(), TestData.OrganizerId))
            .ReturnsAsync(ServiceResult.Ok());
        var controller = CreateController();

        var result = await controller.Create(ValidForm());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StudySessionController.Details), redirect.ActionName);
        _booking.Verify(b => b.CreateSessionAsync(It.IsAny<StudySession>(), TestData.OrganizerId), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_AsOrganizer_RedirectsToIndex()
    {
        LogInAs(TestData.OrganizerId);
        _booking.Setup(b => b.DeleteSessionAsync(1, TestData.OrganizerId, false)).ReturnsAsync(ServiceResult.Ok());
        var controller = CreateController();

        var result = await controller.DeleteConfirmed(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StudySessionController.Index), redirect.ActionName);
    }

    // ---------- Negative ----------

    [Fact]
    public async Task Details_UnknownId_ReturnsNotFound()
    {
        _sessions.Setup(s => s.GetById(99)).ReturnsAsync((StudySession?)null);
        var controller = CreateController();

        var result = await controller.Details(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_InvalidModelState_ReturnsViewAndDoesNotCallService()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError(nameof(StudySessionFormViewModel.Topic), "Enter what you will work on.");
        var form = ValidForm();

        var result = await controller.Create(form);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(form, view.Model);
        _booking.Verify(b => b.CreateSessionAsync(It.IsAny<StudySession>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Create_RoomAlreadyBooked_ReturnsViewWithFieldError()
    {
        LogInAs(TestData.OrganizerId);
        _booking.Setup(b => b.CreateSessionAsync(It.IsAny<StudySession>(), TestData.OrganizerId))
            .ReturnsAsync(ServiceResult.Invalid(nameof(StudySession.StartTime), "The room is already booked."));
        var controller = CreateController();

        var result = await controller.Create(ValidForm());

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(StudySession.StartTime)));
    }

    [Fact]
    public async Task Edit_Get_ByStudentWhoIsNotOrganizer_ReturnsForbid()
    {
        _sessions.Setup(s => s.GetById(1)).ReturnsAsync(TestData.Session());
        LogInAs(TestData.OtherStudentId);
        _booking.Setup(b => b.CanManageSession(It.IsAny<StudySession>(), TestData.OtherStudentId, false)).Returns(false);
        var controller = CreateController();

        var result = await controller.Edit(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Join_WhenServiceRejects_RedirectsWithErrorMessage()
    {
        LogInAs(TestData.OtherStudentId);
        _booking.Setup(b => b.JoinSessionAsync(1, TestData.OtherStudentId))
            .ReturnsAsync(ServiceResult.Invalid(string.Empty, "This session is full."));
        var controller = CreateController();

        var result = await controller.Join(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(StudySessionController.Details), redirect.ActionName);
        Assert.Equal("This session is full.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
    {
        var controller = CreateController();
        var form = ValidForm();
        form.StudySessionId = 2;

        var result = await controller.Edit(1, form);

        Assert.IsType<BadRequestResult>(result);
    }
}
