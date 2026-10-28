using Microsoft.EntityFrameworkCore;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Data access for rooms, with error handling and logging.
public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<RoomRepository> _logger;

    public RoomRepository(AppDbContext db, ILogger<RoomRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Room>?> GetAll()
    {
        try
        {
            // AsNoTracking: read-only query, avoids tracking conflicts in later updates
            return await _db.Rooms
                .AsNoTracking()
                .Include(r => r.StudySessions)
                .OrderBy(r => r.Building).ThenBy(r => r.Name)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[RoomRepository] GetAll() failed");
            return null;
        }
    }

    public async Task<Room?> GetById(int id)
    {
        try
        {
            return await _db.Rooms
                .AsNoTracking()
                .Include(r => r.StudySessions).ThenInclude(s => s.Participations)
                .Include(r => r.StudySessions).ThenInclude(s => s.Organizer)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.RoomId == id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[RoomRepository] GetById() failed for RoomId {RoomId}", id);
            return null;
        }
    }

    public async Task<bool> Create(Room room)
    {
        try
        {
            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();
            _logger.LogInformation("[RoomRepository] Created room {RoomId}", room.RoomId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[RoomRepository] Create() failed for room {Name}", room.Name);
            return false;
        }
    }

    public async Task<bool> Update(Room room)
    {
        try
        {
            var existing = await _db.Rooms.FindAsync(room.RoomId);
            if (existing == null)
            {
                _logger.LogWarning("[RoomRepository] Update(): room {RoomId} not found", room.RoomId);
                return false;
            }

            existing.Name = room.Name;
            existing.Building = room.Building;
            existing.Capacity = room.Capacity;
            existing.HasScreen = room.HasScreen;
            await _db.SaveChangesAsync();
            _logger.LogInformation("[RoomRepository] Updated room {RoomId}", room.RoomId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[RoomRepository] Update() failed for RoomId {RoomId}", room.RoomId);
            return false;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var room = await _db.Rooms.FindAsync(id);
            if (room == null)
            {
                _logger.LogWarning("[RoomRepository] Delete(): room {RoomId} not found", id);
                return false;
            }

            if (await _db.StudySessions.AnyAsync(s => s.RoomId == id))
            {
                _logger.LogWarning("[RoomRepository] Delete(): room {RoomId} still has bookings", id);
                return false;
            }

            _db.Rooms.Remove(room);
            await _db.SaveChangesAsync();
            _logger.LogInformation("[RoomRepository] Deleted room {RoomId}", id);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[RoomRepository] Delete() failed for RoomId {RoomId}", id);
            return false;
        }
    }
}
