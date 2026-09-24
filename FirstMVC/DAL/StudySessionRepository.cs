using Microsoft.EntityFrameworkCore;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Database access for study sessions, with error handling and logging.
//  Methods return null/false on failure instead of throwing, so controllers can show a friendly message.
public class StudySessionRepository : IStudySessionRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<StudySessionRepository> _logger;

    public StudySessionRepository(AppDbContext db, ILogger<StudySessionRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<StudySession>?> GetAll(string? search = null)
    {
        try
        {
            var query = _db.StudySessions.AsNoTracking().Include(s => s.Room).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                // LIKE is case-insensitive in SQLite, so "itpe" also finds "ITPE3200"
                var pattern = $"%{search.Trim()}%";
                query = query.Where(s =>
                    EF.Functions.Like(s.CourseCode, pattern) ||
                    EF.Functions.Like(s.Subject, pattern) ||
                    EF.Functions.Like(s.Topic, pattern));
            }

            return await query.OrderBy(s => s.StartTime).ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] GetAll() failed for search '{Search}'", search);
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
                .Where(s => s.EndTime >= now)
                .OrderBy(s => s.StartTime)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] GetUpcoming() failed");
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
            _db.StudySessions.Add(session);
            await _db.SaveChangesAsync();
            _logger.LogInformation("[StudySessionRepository] Created session {Id} in room {RoomId}",
                session.StudySessionId, session.RoomId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudySessionRepository] Create() failed for session {@Session}", session);
            return false;
        }
    }

    public async Task<bool> Update(StudySession session)
    {
        try
        {
            _db.StudySessions.Update(session);
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

    /// <summary>
    /// Returns true if the room already has a session overlapping the given time span.
    /// Two spans overlap when each one starts before the other ends.
    /// excludeSessionId is used when editing, so a session does not conflict with itself.
    /// </summary>
    public async Task<bool> HasConflict(int roomId, DateTime start, DateTime end, int? excludeSessionId = null)
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
            _logger.LogError(e, "[StudySessionRepository] HasConflict() failed for room {RoomId}", roomId);
            return true; // fail safe: block the booking if availability cannot be verified
        }
    }
}