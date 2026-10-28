using StudyRoomBooking.Models;

namespace StudyRoomBooking.Tests;

/// <summary>Builders for test objects, so each test only states what matters to it.</summary>
public static class TestData
{
    public const int OrganizerId = 10;
    public const int OtherStudentId = 20;

    public static Room Room(int capacity = 6) => new()
    {
        RoomId = 1,
        Name = "Group room 4.12",
        Building = "Pilestredet 35",
        Capacity = capacity
    };

    /// <summary>A session tomorrow 10–12 in room 1, with the organizer already on the participant list.</summary>
    public static StudySession Session(int id = 1, int maxParticipants = 4, params int[] extraParticipants)
    {
        var start = DateTime.Today.AddDays(1).AddHours(10);
        var session = new StudySession
        {
            StudySessionId = id,
            CourseCode = "ITPE3200",
            Subject = "Web Applications",
            Topic = "Repository pattern",
            StartTime = start,
            EndTime = start.AddHours(2),
            MaxParticipants = maxParticipants,
            RoomId = 1,
            OrganizerId = OrganizerId
        };

        session.Participations.Add(new Participation { StudentId = OrganizerId, StudySessionId = id });
        foreach (var studentId in extraParticipants)
            session.Participations.Add(new Participation { StudentId = studentId, StudySessionId = id });

        return session;
    }

    public static Student Student(int id) => new() { StudentId = id, UserId = $"user-{id}", DisplayName = $"Student {id}" };
}
