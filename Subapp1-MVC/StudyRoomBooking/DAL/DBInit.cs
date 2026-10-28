using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Creates the database on startup and adds roles, demo users, rooms and sample sessions the first time.
// Demo logins (also listed in the README and documentation):
//   admin@studyroom.no / Admin1234      (Admin and Student)
//   ingrid@studyroom.no / Student1234   (Student)
//   amir@studyroom.no / Student1234     (Student)
public static class DBInit
{
    public static async Task SeedAsync(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // During development, uncomment to reset the database on every start:
            // await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            await SeedRolesAsync(roleManager);

            var admin = await SeedUserAsync(userManager, db, "admin@studyroom.no", "Admin1234", "Admin", "Study administration", Roles.Admin, Roles.Student);
            var ingrid = await SeedUserAsync(userManager, db, "ingrid@studyroom.no", "Student1234", "Ingrid", "Information Technology", Roles.Student);
            var amir = await SeedUserAsync(userManager, db, "amir@studyroom.no", "Student1234", "Amir", "Applied Computer Science", Roles.Student);

            await SeedRoomsAsync(db);

            // Remove this line to start without sample sessions
            await SeedSessionsAsync(db, ingrid, amir, admin);

            logger.LogInformation("[DBInit] Database ready");
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "[DBInit] Failed to create or seed the database");
            throw; // the app cannot run without a database
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { Roles.Admin, Roles.Student })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // Creates the login (if missing) and its student profile, and returns the profile.
    private static async Task<Student> SeedUserAsync(UserManager<IdentityUser> userManager, AppDbContext db,
        string email, string password, string displayName, string studyProgram, params string[] roles)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not create seed user {email}: {errors}");
            }
            await userManager.AddToRolesAsync(user, roles);
        }

        var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (student == null)
        {
            student = new Student { UserId = user.Id, DisplayName = displayName, StudyProgram = studyProgram };
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }
        return student;
    }

    private static async Task SeedRoomsAsync(AppDbContext db)
    {
        if (await db.Rooms.AnyAsync()) return;

        db.Rooms.AddRange(
            new Room { Name = "Group room 4.12", Building = "Pilestredet 35", Capacity = 6, HasScreen = true },
            new Room { Name = "Group room 4.14", Building = "Pilestredet 35", Capacity = 4, HasScreen = false },
            new Room { Name = "Study room B210", Building = "Pilestredet 32", Capacity = 8, HasScreen = true },
            new Room { Name = "Library group room 3", Building = "Learning Centre, P46", Capacity = 10, HasScreen = true },
            new Room { Name = "Study room K120", Building = "Kjeller campus", Capacity = 6, HasScreen = false }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedSessionsAsync(AppDbContext db, Student ingrid, Student amir, Student admin)
    {
        if (await db.StudySessions.AnyAsync()) return;

        var rooms = await db.Rooms.OrderBy(r => r.RoomId).ToListAsync();
        var day = DateTime.Today;

        var webSession = new StudySession
        {
            CourseCode = "ITPE3200", Subject = "Web Applications", Topic = "Repository pattern and EF Core",
            Description = "Going through the lecture demo together and fixing our own projects.",
            StartTime = day.AddDays(1).AddHours(10), EndTime = day.AddDays(1).AddHours(12),
            MaxParticipants = 6, RoomId = rooms[0].RoomId, OrganizerId = ingrid.StudentId
        };
        webSession.Participations.Add(new Participation { StudentId = ingrid.StudentId });
        webSession.Participations.Add(new Participation { StudentId = amir.StudentId });
        webSession.Messages.Add(new Message { StudentId = ingrid.StudentId, Content = "Bring your laptop with the project open. We start with the DAL." });
        webSession.Messages.Add(new Message { StudentId = amir.StudentId, Content = "I might be 10 minutes late, save me a seat!" });

        var networkSession = new StudySession
        {
            CourseCode = "DATA2410", Subject = "Networking and Cloud Computing", Topic = "Subnetting exercises",
            StartTime = day.AddDays(1).AddHours(13), EndTime = day.AddDays(1).AddHours(15),
            MaxParticipants = 2, RoomId = rooms[1].RoomId, OrganizerId = amir.StudentId
        };
        networkSession.Participations.Add(new Participation { StudentId = amir.StudentId });
        networkSession.Participations.Add(new Participation { StudentId = admin.StudentId }); // full session

        var programmingSession = new StudySession
        {
            CourseCode = "DAPE1400", Subject = "Programming", Topic = "Exam prep: loops and recursion",
            Description = "Bring old exam sets. Beginners welcome.",
            StartTime = day.AddDays(2).AddHours(9), EndTime = day.AddDays(2).AddHours(12),
            MaxParticipants = 8, RoomId = rooms[2].RoomId, OrganizerId = ingrid.StudentId
        };
        programmingSession.Participations.Add(new Participation { StudentId = ingrid.StudentId });

        var validationSession = new StudySession
        {
            CourseCode = "ITPE3200", Subject = "Web Applications", Topic = "Server-side validation",
            StartTime = day.AddDays(3).AddHours(14), EndTime = day.AddDays(3).AddHours(16),
            MaxParticipants = 10, RoomId = rooms[3].RoomId, OrganizerId = amir.StudentId
        };
        validationSession.Participations.Add(new Participation { StudentId = amir.StudentId });

        db.StudySessions.AddRange(webSession, networkSession, programmingSession, validationSession);
        await db.SaveChangesAsync();
    }
}
