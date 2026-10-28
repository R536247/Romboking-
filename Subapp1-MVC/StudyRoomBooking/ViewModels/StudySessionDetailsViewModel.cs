using StudyRoomBooking.Models;

namespace StudyRoomBooking.ViewModels;

// Everything the session details page needs, including flags that decide
// which buttons and forms are shown (conditional rendering).
public class StudySessionDetailsViewModel
{
    public StudySession Session { get; set; } = new();

    public int? CurrentStudentId { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsParticipant { get; set; }
    public bool IsOrganizer { get; set; }
    public bool CanManage { get; set; }

    public MessageFormViewModel NewMessage { get; set; } = new();

    public bool IsFull => Session.IsFull;
    public bool IsFinished => Session.IsFinished;
    public int SpotsLeft => Math.Max(0, Session.MaxParticipants - Session.Participations.Count);
    public bool CanJoin => IsAuthenticated && !IsParticipant && !IsFull && !IsFinished;

    public IEnumerable<Participation> Participants => Session.Participations.OrderBy(p => p.JoinedAt);
    public IEnumerable<Message> Messages => Session.Messages.OrderBy(m => m.CreatedAt);

    public bool CanEditMessage(Message message) => CurrentStudentId == message.StudentId;
    public bool CanDeleteMessage(Message message) => IsAdmin || CurrentStudentId == message.StudentId;
}
