using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Data access for student profiles.
public interface IStudentRepository
{
    Task<Student?> GetByUserId(string userId);
    Task<Student?> GetById(int id);
    Task<bool> Create(Student student);
    Task<bool> Update(Student student);
}
