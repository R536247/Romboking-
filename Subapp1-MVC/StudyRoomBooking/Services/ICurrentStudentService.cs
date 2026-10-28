using StudyRoomBooking.Models;

namespace StudyRoomBooking.Services;

//Gives controllers the logged-in student without depending on HttpContext,
// which keeps the controllers easy to unit test.
public interface ICurrentStudentService
{
    bool IsAdmin { get; }
    Task<Student?> GetCurrentStudentAsync();
}
