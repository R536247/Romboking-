using Microsoft.EntityFrameworkCore;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Data access for student profiles, with error handling and logging.
public class StudentRepository : IStudentRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<StudentRepository> _logger;

    public StudentRepository(AppDbContext db, ILogger<StudentRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Student?> GetByUserId(string userId)
    {
        try
        {
            return await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudentRepository] GetByUserId() failed for user {UserId}", userId);
            return null;
        }
    }

    public async Task<Student?> GetById(int id)
    {
        try
        {
            return await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudentRepository] GetById() failed for StudentId {Id}", id);
            return null;
        }
    }

    public async Task<bool> Create(Student student)
    {
        try
        {
            _db.Students.Add(student);
            await _db.SaveChangesAsync();
            _logger.LogInformation("[StudentRepository] Created student profile {Id}", student.StudentId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudentRepository] Create() failed for user {UserId}", student.UserId);
            return false;
        }
    }

    public async Task<bool> Update(Student student)
    {
        try
        {
            var existing = await _db.Students.FindAsync(student.StudentId);
            if (existing == null)
            {
                _logger.LogWarning("[StudentRepository] Update(): student {Id} not found", student.StudentId);
                return false;
            }

            existing.DisplayName = student.DisplayName;
            existing.StudyProgram = student.StudyProgram;
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[StudentRepository] Update() failed for StudentId {Id}", student.StudentId);
            return false;
        }
    }
}
