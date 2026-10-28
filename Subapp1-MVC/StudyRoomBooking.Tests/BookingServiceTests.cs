using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;
using StudyRoomBooking.Services;

namespace StudyRoomBooking.Tests;

/// <summary>Tests of the domain rules in BookingService. Repositories are mocked, so no database is used.</summary>
public class BookingServiceTests
{
    private readonly Mock<IStudySessionRepository> _sessions = new();
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IParticipationRepository> _participations = new();
    private readonly Mock<IMessageRepository> _messages = new();
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _service = new BookingService(_sessions.Object, _rooms.Object, _participations.Object,
            _messages.Object, NullLogger<BookingService>.Instance);
    }

    /// <summary>Arranges a free room with no conflicts for create/update tests.</summary>
    private void ArrangeFreeRoom(int capacity = 6)
    {
        _rooms.Setup(r => r.GetById(1)).ReturnsAsync(TestData.Room(capacity));
        _sessions.Setup(s => s.HasRoomConflict(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
        _participations.Setup(p => p.HasOverlap(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
    }

    private static StudySession NewBooking(int maxParticipants = 4)
    {
        var session = TestData.Session(id: 0, maxParticipants: maxParticipants);
        session.Participations.Clear(); // a new booking has no participants yet
        return session;
    }

    // ---------- Positive ----------

    [Fact]
    public async Task CreateSession_ValidBooking_SucceedsAndAddsOrganizerAsParticipant()
    {
        ArrangeFreeRoom();
        _sessions.Setup(s => s.Create(It.IsAny<StudySession>())).ReturnsAsync(true);
        var session = NewBooking();

        var result = await _service.CreateSessionAsync(session, TestData.OrganizerId);

        Assert.True(result.Succeeded);
        Assert.Equal(TestData.OrganizerId, session.OrganizerId);
        var participation = Assert.Single(session.Participations);
        Assert.Equal(TestData.OrganizerId, participation.StudentId);
        _sessions.Verify(s => s.Create(session), Times.Once);
    }

    [Fact]
    public async Task JoinSession_WithFreeSpot_SucceedsAndAddsParticipation()
    {
        _sessions.Setup(s => s.GetById(1)).ReturnsAsync(TestData.Session(maxParticipants: 4));
        _participations.Setup(p => p.HasOverlap(TestData.OtherStudentId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
            .ReturnsAsync(false);
        _participations.Setup(p => p.Add(It.IsAny<Participation>())).ReturnsAsync(true);

        var result = await _service.JoinSessionAsync(1, TestData.OtherStudentId);

        Assert.True(result.Succeeded);
        _participations.Verify(p => p.Add(It.Is<Participation>(x =>
            x.StudentId == TestData.OtherStudentId && x.StudySessionId == 1)), Times.Once);
    }

    [Fact]
    public async Task LeaveSession_AsParticipant_Succeeds()
    {
        _sessions.Setup(s => s.GetById(1)).ReturnsAsync(TestData.Session(1, 4, TestData.OtherStudentId));
        _participations.Setup(p => p.Remove(TestData.OtherStudentId, 1)).ReturnsAsync(true);

        var result = await _service.LeaveSessionAsync(1, TestData.OtherStudentId);

        Assert.True(result.Succeeded);
        _participations.Verify(p => p.Remove(TestData.OtherStudentId, 1), Times.Once);
    }

    [Fact]
    public async Task DeleteMessage_ByAdmin_Succeeds()
    {
        _messages.Setup(m => m.GetById(5)).ReturnsAsync(new Message { MessageId = 5, StudentId = TestData.OrganizerId, StudySessionId = 1 });
        _messages.Setup(m => m.Delete(5)).ReturnsAsync(true);

        var result = await _service.DeleteMessageAsync(5, TestData.OtherStudentId, isAdmin: true);

        Assert.True(result.Succeeded);
        _messages.Verify(m => m.Delete(5), Times.Once);
    }

    // ---------- Negative ----------

    [Fact]
    public async Task CreateSession_RoomAlreadyBooked_ReturnsInvalidAndDoesNotSave()
    {
        ArrangeFreeRoom();
        _sessions.Setup(s => s.HasRoomConflict(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var result = await _service.CreateSessionAsync(NewBooking(), TestData.OrganizerId);

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Contains(result.Errors, e => e.Field == nameof(StudySession.StartTime));
        _sessions.Verify(s => s.Create(It.IsAny<StudySession>()), Times.Never);
    }

    [Fact]
    public async Task CreateSession_MoreParticipantsThanRoomCapacity_ReturnsInvalid()
    {
        ArrangeFreeRoom(capacity: 4);

        var result = await _service.CreateSessionAsync(NewBooking(maxParticipants: 8), TestData.OrganizerId);

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Contains(result.Errors, e => e.Field == nameof(StudySession.MaxParticipants));
        _sessions.Verify(s => s.Create(It.IsAny<StudySession>()), Times.Never);
    }

    [Fact]
    public async Task JoinSession_WhenFull_ReturnsInvalid()
    {
        // Max 2: the organizer and student 11 already fill it
        _sessions.Setup(s => s.GetById(1)).ReturnsAsync(TestData.Session(1, 2, 11));

        var result = await _service.JoinSessionAsync(1, TestData.OtherStudentId);

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Contains("full", result.FirstError);
        _participations.Verify(p => p.Add(It.IsAny<Participation>()), Times.Never);
    }

    [Fact]
    public async Task JoinSession_OverlapsAnotherOfStudentsSessions_ReturnsInvalid()
    {
        _sessions.Setup(s => s.GetById(1)).ReturnsAsync(TestData.Session(maxParticipants: 4));
        _participations.Setup(p => p.HasOverlap(TestData.OtherStudentId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        var result = await _service.JoinSessionAsync(1, TestData.OtherStudentId);

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        _participations.Verify(p => p.Add(It.IsAny<Participation>()), Times.Never);
    }

    [Fact]
    public async Task LeaveSession_AsOrganizer_ReturnsInvalid()
    {
        _sessions.Setup(s => s.GetById(1)).ReturnsAsync(TestData.Session());

        var result = await _service.LeaveSessionAsync(1, TestData.OrganizerId);

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        _participations.Verify(p => p.Remove(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMessage_ByAnotherStudent_ReturnsForbidden()
    {
        _messages.Setup(m => m.GetById(5)).ReturnsAsync(new Message { MessageId = 5, StudentId = TestData.OrganizerId, StudySessionId = 1 });

        var result = await _service.DeleteMessageAsync(5, TestData.OtherStudentId, isAdmin: false);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        _messages.Verify(m => m.Delete(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSession_ByAnotherStudent_ReturnsForbidden()
    {
        _sessions.Setup(s => s.GetById(1)).ReturnsAsync(TestData.Session());

        var result = await _service.UpdateSessionAsync(TestData.Session(), TestData.OtherStudentId, isAdmin: false);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        _sessions.Verify(s => s.Update(It.IsAny<StudySession>()), Times.Never);
    }
}
