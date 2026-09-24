using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// CRUD, search and booking-conflict check for study sessions
public interface IStudySessionRepository
{
    Task<IEnumerable<StudySession>?> GetAll(string? search = null);
    Task<IEnumerable<StudySession>?> GetUpcoming(int count);
    Task<StudySession?> GetById(int id);
    Task<bool> Create(StudySession session);
    Task<bool> Update(StudySession session);
    Task<bool> Delete(int id);
    Task<bool> HasConflict(int roomId, DateTime start, DateTime end, int? excludeSessionId = null);
}