using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// CRUD for rooms.
public interface IRoomRepository
{
    Task<IEnumerable<Room>?> GetAll();
    Task<Room?> GetById(int id);
    Task<bool> Create(Room room);
    Task<bool> Update(Room room);
    Task<bool> Delete(int id);
}
