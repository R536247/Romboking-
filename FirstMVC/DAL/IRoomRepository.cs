using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Read operations for rooms. Rooms are managed through seed data in the MVP
public interface IRoomRepository
{
    Task<IEnumerable<Room>?> GetAll();
    Task<Room?> GetById(int id);
}