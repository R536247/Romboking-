using Microsoft.EntityFrameworkCore;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Database access for rooms, with error handling and logging
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
            // AsNoTracking: read-only query, avoids tracking conflicts when a session is updated later
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
                .Include(r => r.StudySessions.OrderBy(s => s.StartTime))
                .FirstOrDefaultAsync(r => r.RoomId == id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[RoomRepository] GetById() failed for RoomId {RoomId}", id);
            return null;
        }
    }
}