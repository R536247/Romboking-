using Microsoft.EntityFrameworkCore;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Data access for study sessions, with error handling and logging.
// Methods return null/false on failure instead of throwing, so callers can show a friendly message.
public class StudySessionRepository : IStudySessionRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<StudySessionRepository> _logger;

    public StudySessionRepository(AppDbContext db, ILogger<StudySessionRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<StudySession>?> GetFiltered(SessionQuery query)
    {
        try
        {
            IQueryable<StudySession> sessions = _db.StudySessions
                .AsNoTracking()
                .Include(s => s.Room)
                .Include(s => s.Organizer)
                .Include(s => s.Participations);

            if (!query.IncludeFinished)
            {
                var now = DateTime.Now;
                sessions = sessions.Where(s => s.EndTime >= now);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                // LIKE is case-insensitive in SQLite, so "itpe" also finds "ITPE3200"
                var pattern = $"%{query.Search.Trim()}%";
                sessions = sessions.Where(s =>
                    EF.Functions.Like(s.CourseCode, pattern) ||
                    EF.Functions.Like(s.Subject, pattern) ||
                    EF.Functions.Like(s.Topic, pattern));
            }

            if (!string.IsNullOrWhiteSpace(query.Building))
            {
                var building = query.Building;
                sessions = sessions.Where(s => s.Room!.Building == building);
            }

            if (query.RoomId.HasValue)
            {
                var roomId = query.RoomId.Value;
                sessions = sessions.Where(s => s.RoomId == roomId);
            }

            if (query.Date.HasValue)
            {
                var dayStart = query.Date.Value.Date;
                var dayEnd = dayStart.AddDays(1);
                sessions = sessions.Where(s => s.StartTime >= dayStart && s.StartTime < dayEnd);
            }

            if (query.OnlyAvailable)
            {
                sessions = sessions.Where(s => s.Participations.Count < s.MaxParticipants);
            }

            return await sessions
                .OrderBy(s => s.StartTime)
                .AsSplitQuery()
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] GetFiltered() failed for search '{Search}'", query.Search);
            return null;
        }
    }

    public async Task<IEnumerable<StudySession>?> GetUpcoming(int count)
    {
        try
        {
            var now = DateTime.Now;
            return await _db.StudySessions
                .AsNoTracking()
                .Include(s => s.Room)
                .Include(s => s.Organizer)
                .Include(s => s.Participations)
                .Where(s => s.EndTime >= now)
                .OrderBy(s => s.StartTime)
                .Take(count)
                .AsSplitQuery()
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] GetUpcoming() failed");
            return null;
        }
    }

    public async Task<IEnumerable<StudySession>?> GetForStudent(int studentId)
    {
        try
        {
            return await _db.StudySessions
                .AsNoTracking()
                .Include(s => s.Room)
                .Include(s => s.Organizer)
                .Include(s => s.Participations)
                .Where(s => s.Participations.Any(p => p.StudentId == studentId))
                .OrderBy(s => s.StartTime)
                .AsSplitQuery()
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] GetForStudent() failed for StudentId {Id}", studentId);
            return null;
        }
    }

    public async Task<StudySession?> GetById(int id)
    {
        try
        {
            return await _db.StudySessions
                .AsNoTracking()
                .Include(s => s.Room)
                .Include(s => s.Organizer)
                .Include(s => s.Participations).ThenInclude(p => p.Student)
                .Include(s => s.Messages).ThenInclude(m => m.Student)
                .AsSplitQuery()
                .FirstOrDefaultAsync(s => s.StudySessionId == id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] GetById() failed for StudySessionId {Id}", id);
            return null;
        }
    }

    public async Task<bool> Create(StudySession session)
    {
        try
        {
            // Participations added to session.Participations are inserted in the same transaction
            _db.StudySessions.Add(session);
            await _db.SaveChangesAsync();
            _logger.LogInformation("[StudySessionRepository] Created session {Id} in room {RoomId}",
                session.StudySessionId, session.RoomId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] Create() failed for course {Course} in room {RoomId}",
                session.CourseCode, session.RoomId);
            return false;
        }
    }

    public async Task<bool> Update(StudySession session)
    {
        try
        {
            var existing = await _db.StudySessions.FindAsync(session.StudySessionId);
            if (existing == null)
            {
                _logger.LogWarning("[StudySessionRepository] Update(): session {Id} not found", session.StudySessionId);
                return false;
            }

            // Copy only the editable fields; organizer and relations stay as they are
            existing.CourseCode = session.CourseCode;
            existing.Subject = session.Subject;
            existing.Topic = session.Topic;
            existing.Description = session.Description;
            existing.StartTime = session.StartTime;
            existing.EndTime = session.EndTime;
            existing.MaxParticipants = session.MaxParticipants;
            existing.RoomId = session.RoomId;

            await _db.SaveChangesAsync();
            _logger.LogInformation("[StudySessionRepository] Updated session {Id}", session.StudySessionId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] Update() failed for StudySessionId {Id}", session.StudySessionId);
            return false;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var session = await _db.StudySessions.FindAsync(id);
            if (session == null)
            {
                _logger.LogWarning("[StudySessionRepository] Delete(): session {Id} not found", id);
                return false;
            }

            // Participations and messages are removed by cascade delete
            _db.StudySessions.Remove(session);
            await _db.SaveChangesAsync();
            _logger.LogInformation("[StudySessionRepository] Deleted session {Id}", id);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] Delete() failed for StudySessionId {Id}", id);
            return false;
        }
    }

    // True if the room already has a session overlapping the given time span.
    // Two spans overlap when each one starts before the other ends.
    // excludeSessionId is used when editing, so a session does not conflict with itself.
    public async Task<bool> HasRoomConflict(int roomId, DateTime start, DateTime end, int? excludeSessionId = null)
    {
        try
        {
            int excludeId = excludeSessionId ?? 0; // ids start at 1, so 0 excludes nothing
            return await _db.StudySessions.AnyAsync(s =>
                s.RoomId == roomId &&
                s.StudySessionId != excludeId &&
                s.StartTime < end &&
                start < s.EndTime);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] HasRoomConflict() failed for room {RoomId}", roomId);
            return true; // fail safe: block the booking if availability cannot be verified
        }
    }
}
