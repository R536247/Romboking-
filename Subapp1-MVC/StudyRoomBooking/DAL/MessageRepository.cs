using Microsoft.EntityFrameworkCore;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Data access for messages, with error handling and logging.
public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<MessageRepository> _logger;

    public MessageRepository(AppDbContext db, ILogger<MessageRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Message?> GetById(int id)
    {
        try
        {
            return await _db.Messages
                .AsNoTracking()
                .Include(m => m.Student)
                .FirstOrDefaultAsync(m => m.MessageId == id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[MessageRepository] GetById() failed for MessageId {Id}", id);
            return null;
        }
    }

    public async Task<bool> Create(Message message)
    {
        try
        {
            _db.Messages.Add(message);
            await _db.SaveChangesAsync();
            _logger.LogInformation("[MessageRepository] Student {StudentId} posted message {Id} in session {SessionId}",
                message.StudentId, message.MessageId, message.StudySessionId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[MessageRepository] Create() failed in session {SessionId}", message.StudySessionId);
            return false;
        }
    }

    public async Task<bool> Update(Message message)
    {
        try
        {
            var existing = await _db.Messages.FindAsync(message.MessageId);
            if (existing == null)
            {
                _logger.LogWarning("[MessageRepository] Update(): message {Id} not found", message.MessageId);
                return false;
            }

            existing.Content = message.Content;
            existing.EditedAt = message.EditedAt;
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[MessageRepository] Update() failed for MessageId {Id}", message.MessageId);
            return false;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var message = await _db.Messages.FindAsync(id);
            if (message == null)
            {
                _logger.LogWarning("[MessageRepository] Delete(): message {Id} not found", id);
                return false;
            }

            _db.Messages.Remove(message);
            await _db.SaveChangesAsync();
            _logger.LogInformation("[MessageRepository] Deleted message {Id}", id);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[MessageRepository] Delete() failed for MessageId {Id}", id);
            return false;
        }
    }
}
