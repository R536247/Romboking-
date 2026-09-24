using StudyRoomBooking.Models;

namespace StudyRoomBooking.DAL;

// Creates the database on startup and fills it with sample data the first time
public static class DBInit
{
    public static void Seed(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // During development, uncomment to reset the database on every start:
            // db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            if (!db.Rooms.Any())
            {
                db.Rooms.AddRange(
                    new Room { Name = "Group room 4.12", Building = "Pilestredet 35", Capacity = 6, HasScreen = true },
                    new Room { Name = "Group room 4.14", Building = "Pilestredet 35", Capacity = 4, HasScreen = false },
                    new Room { Name = "Study room B210", Building = "Pilestredet 32", Capacity = 8, HasScreen = true },
                    new Room { Name = "Library group room 3", Building = "Learning Centre, P46", Capacity = 10, HasScreen = true },
                    new Room { Name = "Study room K120", Building = "Kjeller campus", Capacity = 6, HasScreen = false }
                );
                db.SaveChanges();
                logger.LogInformation("[DBInit] Seeded rooms");
            }

            if (!db.StudySessions.Any())
            {
                var rooms = db.Rooms.OrderBy(r => r.RoomId).ToList();
                var day = DateTime.Today;

                db.StudySessions.AddRange(
                    new StudySession
                    {
                        CourseCode = "ITPE3200", Subject = "Web Applications", Topic = "Repository pattern and EF Core",
                        Description = "Going through the lecture demo together and fixing our own projects.",
                        StartTime = day.AddDays(1).AddHours(10), EndTime = day.AddDays(1).AddHours(12),
                        MaxParticipants = 6, OrganizerName = "Ingrid", RoomId = rooms[0].RoomId
                    },
                    new StudySession
                    {
                        CourseCode = "DATA2410", Subject = "Networking and Cloud Computing", Topic = "Subnetting exercises",
                        StartTime = day.AddDays(1).AddHours(13), EndTime = day.AddDays(1).AddHours(15),
                        MaxParticipants = 4, OrganizerName = "Amir", RoomId = rooms[1].RoomId
                    },
                    new StudySession
                    {
                        CourseCode = "DAPE1400", Subject = "Programming", Topic = "Exam prep: loops and recursion",
                        Description = "Bring old exam sets. Beginners welcome.",
                        StartTime = day.AddDays(2).AddHours(9), EndTime = day.AddDays(2).AddHours(12),
                        MaxParticipants = 8, OrganizerName = "Sofie", RoomId = rooms[2].RoomId
                    },
                    new StudySession
                    {
                        CourseCode = "ITPE3200", Subject = "Web Applications", Topic = "Server-side validation",
                        StartTime = day.AddDays(3).AddHours(14), EndTime = day.AddDays(3).AddHours(16),
                        MaxParticipants = 10, OrganizerName = "Jonas", RoomId = rooms[3].RoomId
                    },
                    new StudySession
                    {
                        CourseCode = "DATA1700", Subject = "Web Programming", Topic = "JavaScript and fetch",
                        StartTime = day.AddDays(4).AddHours(11), EndTime = day.AddDays(4).AddHours(13),
                        MaxParticipants = 5, OrganizerName = "Maja", RoomId = rooms[4].RoomId
                    }
                );
                db.SaveChanges();
                logger.LogInformation("[DBInit] Seeded study sessions");
            }
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "[DBInit] Failed to create or seed the database");
            throw; // the app cannot run without a database
        }
    }
}