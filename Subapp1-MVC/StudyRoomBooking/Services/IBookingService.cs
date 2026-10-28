using StudyRoomBooking.Models;

namespace StudyRoomBooking.Services;

// The domain rules for booking, joining and messaging. Controllers call this instead of
// writing to the repositories directly, so every rule is enforced in one place.
public interface IBookingService
{
    bool CanManageSession(StudySession session, int? studentId, bool isAdmin);

    Task<ServiceResult> CreateSessionAsync(StudySession session, int organizerId);
    Task<ServiceResult> UpdateSessionAsync(StudySession changes, int studentId, bool isAdmin);
    Task<ServiceResult> DeleteSessionAsync(int sessionId, int studentId, bool isAdmin);

    Task<ServiceResult> JoinSessionAsync(int sessionId, int studentId);
    Task<ServiceResult> LeaveSessionAsync(int sessionId, int studentId);

    Task<ServiceResult> PostMessageAsync(int sessionId, int studentId, string content);
    Task<ServiceResult> UpdateMessageAsync(int messageId, int studentId, string content);
    Task<ServiceResult> DeleteMessageAsync(int messageId, int studentId, bool isAdmin);
}
