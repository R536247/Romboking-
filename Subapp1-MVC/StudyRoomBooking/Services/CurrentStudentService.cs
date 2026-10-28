using System.Security.Claims;
using StudyRoomBooking.DAL;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.Services;

// Looks up the student profile of the user in the current request. 
public class CurrentStudentService : ICurrentStudentService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IStudentRepository _students;

    public CurrentStudentService(IHttpContextAccessor httpContextAccessor, IStudentRepository students)
    {
        _httpContextAccessor = httpContextAccessor;
        _students = students;
    }

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User.IsInRole(Roles.Admin) ?? false;

    public async Task<Student?> GetCurrentStudentAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;

        return await _students.GetByUserId(userId);
    }
}
