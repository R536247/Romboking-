using Microsoft.EntityFrameworkCore;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Data access for participations (joining and leaving sessions), with error handling and logging.
public class ParticipationRepository : IParticipationRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<ParticipationRepository> _logger;

    public ParticipationRepository(AppDbContext db, ILogger<ParticipationRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> Add(Participation participation)
    {
        try
        {
            _db.Participations.Add(participation);
            await _db.SaveChangesAsync();
            _logger.LogInformation("[ParticipationRepository] Student {StudentId} joined session {SessionId}",
                participation.StudentId, participation.StudySessionId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[ParticipationRepository] Add() failed for student {StudentId} in session {SessionId}",
                participation.StudentId, participation.StudySessionId);
            return false;
        }
    }

    public async Task<bool> Remove(int studentId, int studySessionId)
    {
        try
        {
            // Composite key order matches HasKey in AppDbContext: (StudentId, StudySessionId)
            var participation = await _db.Participations.FindAsync(studentId, studySessionId);
            if (participation == null)
            {
                _logger.LogWarning("[ParticipationRepository] Remove(): student {StudentId} is not in session {SessionId}",
                    studentId, studySessionId);
                return false;
            }

            _db.Participations.Remove(participation);
            await _db.SaveChangesAsync();
            _logger.LogInformation("[ParticipationRepository] Student {StudentId} left session {SessionId}",
                studentId, studySessionId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[ParticipationRepository] Remove() failed for student {StudentId} in session {SessionId}",
                studentId, studySessionId);
            return false;
        }
    }

    // True if the student already takes part in another session overlapping the time span. 
    public async Task<bool> HasOverlap(int studentId, DateTime start, DateTime end, int? excludeSessionId = null)
    {
        try
        {
            int excludeId = excludeSessionId ?? 0;
            return await _db.Participations.AnyAsync(p =>
                p.StudentId == studentId &&
                p.StudySessionId != excludeId &&
                p.StudySession!.StartTime < end &&
                start < p.StudySession.EndTime);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[ParticipationRepository] HasOverlap() failed for student {StudentId}", studentId);
            return true; // fail safe
        }
    }
}
