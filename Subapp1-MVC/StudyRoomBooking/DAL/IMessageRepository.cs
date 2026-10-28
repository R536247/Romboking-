using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// CRUD for messages in study sessions.
public interface IMessageRepository
{
    Task<Message?> GetById(int id);
    Task<bool> Create(Message message);
    Task<bool> Update(Message message);
    Task<bool> Delete(int id);
}
