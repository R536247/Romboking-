using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// CRUD, filtering and the room-conflict check for study sessions
public interface IStudySessionRepository
{
    Task<IEnumerable<StudySession>?> GetFiltered(SessionQuery query);
    Task<IEnumerable<StudySession>?> GetUpcoming(int count);
    Task<IEnumerable<StudySession>?> GetForStudent(int studentId);
    Task<StudySession?> GetById(int id);
    Task<bool> Create(StudySession session);
    Task<bool> Update(StudySession session);
    Task<bool> Delete(int id);
    Task<bool> HasRoomConflict(int roomId, DateTime start, DateTime end, int? excludeSessionId = null);
}
