using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Data access for the Student–StudySession join table.
public interface IParticipationRepository
{
    Task<bool> Add(Participation participation);
    Task<bool> Remove(int studentId, int studySessionId);
    Task<bool> HasOverlap(int studentId, DateTime start, DateTime end, int? excludeSessionId = null);
}
