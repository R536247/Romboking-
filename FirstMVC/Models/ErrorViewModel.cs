namespace StudyRoomBooking.Models;

// Data shown on the generic error page.
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
