using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.Services;

// Implements the server-side domain rules:
//  1. A room cannot be booked by two sessions in overlapping time spans.
//  2. A session cannot have more places than the room's capacity, or fewer than the students who already joined.
//  3. A student cannot take part in two sessions at the same time (applies to organizers and participants).
//  4. Students cannot join a session that is full, finished, or that they already joined.
//  5. The organizer joins automatically and cannot leave their own session.
//  6. Only the organizer or an admin can edit or delete a session.
//  7. Only participants can post messages; only the author can edit a message; the author or an admin can delete it.
public class BookingService : IBookingService
{
    private readonly IStudySessionRepository _sessions;
    private readonly IRoomRepository _rooms;
    private readonly IParticipationRepository _participations;
    private readonly IMessageRepository _messages;
    private readonly ILogger<BookingService> _logger;

    public BookingService(IStudySessionRepository sessions, IRoomRepository rooms,
        IParticipationRepository participations, IMessageRepository messages, ILogger<BookingService> logger)
    {
        _sessions = sessions;
        _rooms = rooms;
        _participations = participations;
        _messages = messages;
        _logger = logger;
    }

    public bool CanManageSession(StudySession session, int? studentId, bool isAdmin) =>
        isAdmin || (studentId.HasValue && session.OrganizerId == studentId.Value);

    // Sessions

    public async Task<ServiceResult> CreateSessionAsync(StudySession session, int organizerId)
    {
        session.OrganizerId = organizerId;

        var errors = await ValidateBookingAsync(session, organizerId, excludeSessionId: null);
        if (errors.Count > 0)
        {
            _logger.LogInformation("[BookingService] Booking rejected for student {StudentId}: {Errors}",
                organizerId, string.Join("; ", errors.Select(e => e.Message)));
            return ServiceResult.Invalid(errors);
        }

        // Rule 5: the organizer automatically takes part in their own session
        session.Participations.Add(new Participation { StudentId = organizerId, JoinedAt = DateTime.Now });

        if (!await _sessions.Create(session))
            return ServiceResult.Failed("The session could not be saved. Try again in a moment.");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateSessionAsync(StudySession changes, int studentId, bool isAdmin)
    {
        var existing = await _sessions.GetById(changes.StudySessionId);
        if (existing == null)
            return ServiceResult.NotFound("The session does not exist.");

        if (!CanManageSession(existing, studentId, isAdmin))
        {
            _logger.LogWarning("[BookingService] Student {StudentId} tried to edit session {SessionId} without access",
                studentId, changes.StudySessionId);
            return ServiceResult.Forbidden("Only the organizer can edit this session.");
        }

        changes.OrganizerId = existing.OrganizerId;
        var errors = await ValidateBookingAsync(changes, existing.OrganizerId, changes.StudySessionId);

        // Rule 2: cannot shrink below the number of students who already joined
        int joined = existing.Participations.Count;
        if (changes.MaxParticipants < joined)
        {
            errors.Add(new ServiceError(nameof(StudySession.MaxParticipants),
                $"{joined} students have already joined, so max participants cannot be lower than {joined}."));
        }

        if (errors.Count > 0)
            return ServiceResult.Invalid(errors);

        if (!await _sessions.Update(changes))
            return ServiceResult.Failed("The changes could not be saved. Try again in a moment.");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteSessionAsync(int sessionId, int studentId, bool isAdmin)
    {
        var session = await _sessions.GetById(sessionId);
        if (session == null)
            return ServiceResult.NotFound("The session does not exist.");

        if (!CanManageSession(session, studentId, isAdmin))
        {
            _logger.LogWarning("[BookingService] Student {StudentId} tried to delete session {SessionId} without access",
                studentId, sessionId);
            return ServiceResult.Forbidden("Only the organizer can delete this session.");
        }

        if (!await _sessions.Delete(sessionId))
            return ServiceResult.Failed("The session could not be deleted. Try again in a moment.");

        return ServiceResult.Ok();
    }

    // Participation 

    public async Task<ServiceResult> JoinSessionAsync(int sessionId, int studentId)
    {
        var session = await _sessions.GetById(sessionId);
        if (session == null)
            return ServiceResult.NotFound("The session does not exist.");

        if (session.IsFinished)
            return ServiceResult.Invalid(string.Empty, "This session has already finished.");

        if (session.Participations.Any(p => p.StudentId == studentId))
            return ServiceResult.Invalid(string.Empty, "You have already joined this session.");

        if (session.IsFull)
            return ServiceResult.Invalid(string.Empty, "This session is full.");

        if (await _participations.HasOverlap(studentId, session.StartTime, session.EndTime, sessionId))
            return ServiceResult.Invalid(string.Empty, "You are already taking part in another session at this time.");

        var participation = new Participation { StudentId = studentId, StudySessionId = sessionId, JoinedAt = DateTime.Now };
        if (!await _participations.Add(participation))
            return ServiceResult.Failed("Could not join the session. Try again in a moment.");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> LeaveSessionAsync(int sessionId, int studentId)
    {
        var session = await _sessions.GetById(sessionId);
        if (session == null)
            return ServiceResult.NotFound("The session does not exist.");

        if (session.OrganizerId == studentId)
            return ServiceResult.Invalid(string.Empty, "You organize this session and cannot leave it. Delete the session instead.");

        if (!session.Participations.Any(p => p.StudentId == studentId))
            return ServiceResult.Invalid(string.Empty, "You are not taking part in this session.");

        if (!await _participations.Remove(studentId, sessionId))
            return ServiceResult.Failed("Could not leave the session. Try again in a moment.");

        return ServiceResult.Ok();
    }

    // Messages 

    public async Task<ServiceResult> PostMessageAsync(int sessionId, int studentId, string content)
    {
        var session = await _sessions.GetById(sessionId);
        if (session == null)
            return ServiceResult.NotFound("The session does not exist.");

        if (!session.Participations.Any(p => p.StudentId == studentId))
            return ServiceResult.Forbidden("Only participants can write messages in this session.");

        var text = (content ?? string.Empty).Trim();
        if (text.Length == 0)
            return ServiceResult.Invalid(nameof(Message.Content), "Write a message before sending.");

        var message = new Message { StudySessionId = sessionId, StudentId = studentId, Content = text, CreatedAt = DateTime.Now };
        if (!await _messages.Create(message))
            return ServiceResult.Failed("The message could not be posted. Try again in a moment.");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateMessageAsync(int messageId, int studentId, string content)
    {
        var message = await _messages.GetById(messageId);
        if (message == null)
            return ServiceResult.NotFound("The message does not exist.");

        if (message.StudentId != studentId)
            return ServiceResult.Forbidden("You can only edit your own messages.");

        var text = (content ?? string.Empty).Trim();
        if (text.Length == 0)
            return ServiceResult.Invalid(nameof(Message.Content), "The message cannot be empty.");

        message.Content = text;
        message.EditedAt = DateTime.Now;
        if (!await _messages.Update(message))
            return ServiceResult.Failed("The message could not be saved. Try again in a moment.");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteMessageAsync(int messageId, int studentId, bool isAdmin)
    {
        var message = await _messages.GetById(messageId);
        if (message == null)
            return ServiceResult.NotFound("The message does not exist.");

        if (message.StudentId != studentId && !isAdmin)
            return ServiceResult.Forbidden("You can only delete your own messages.");

        if (!await _messages.Delete(messageId))
            return ServiceResult.Failed("The message could not be deleted. Try again in a moment.");

        return ServiceResult.Ok();
    }

    // Shared rule checks 

    // Rules 1–3 for creating or editing a booking. Returns all errors found.
    private async Task<List<ServiceError>> ValidateBookingAsync(StudySession session, int organizerId, int? excludeSessionId)
    {
        var errors = new List<ServiceError>();

        if (session.EndTime <= session.StartTime)
        {
            errors.Add(new ServiceError(nameof(StudySession.EndTime), "The end time must be after the start time."));
            return errors;
        }

        var room = await _rooms.GetById(session.RoomId);
        if (room == null)
        {
            errors.Add(new ServiceError(nameof(StudySession.RoomId), "The selected room does not exist."));
            return errors;
        }

        if (session.MaxParticipants > room.Capacity)
        {
            errors.Add(new ServiceError(nameof(StudySession.MaxParticipants),
                $"{room.Name} has space for {room.Capacity} people. Lower the number or pick a bigger room."));
        }

        if (await _sessions.HasRoomConflict(session.RoomId, session.StartTime, session.EndTime, excludeSessionId))
        {
            errors.Add(new ServiceError(nameof(StudySession.StartTime),
                $"{room.Name} is already booked in this time span. Choose another time or room."));
        }

        if (await _participations.HasOverlap(organizerId, session.StartTime, session.EndTime, excludeSessionId))
        {
            errors.Add(new ServiceError(nameof(StudySession.StartTime),
                "You are already taking part in another session at this time."));
        }

        return errors;
    }
}
