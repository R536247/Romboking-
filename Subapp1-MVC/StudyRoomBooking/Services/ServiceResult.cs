namespace StudyRoomBooking.Services;

// Outcome of a business operation, so controllers can react without exceptions.
public enum ServiceStatus
{
    Ok,
    Invalid,    // a rule or input was rejected: show the errors in the form
    NotFound,   // the item does not exist: 404
    Forbidden,  // the user may not do this: 403
    Failed      // the database operation failed: show a general error
}

// One error. Field is the form property it belongs to, or empty for a general error.
public record ServiceError(string Field, string Message);

public class ServiceResult
{
    private ServiceResult(ServiceStatus status)
    {
        Status = status;
    }

    public ServiceStatus Status { get; }
    public List<ServiceError> Errors { get; } = new();
    public bool Succeeded => Status == ServiceStatus.Ok;
    public string FirstError => Errors.Count > 0 ? Errors[0].Message : string.Empty;

    public static ServiceResult Ok() => new(ServiceStatus.Ok);

    public static ServiceResult Invalid(string field, string message) =>
        WithError(ServiceStatus.Invalid, field, message);

    public static ServiceResult Invalid(IEnumerable<ServiceError> errors)
    {
        var result = new ServiceResult(ServiceStatus.Invalid);
        result.Errors.AddRange(errors);
        return result;
    }

    public static ServiceResult NotFound(string message) => WithError(ServiceStatus.NotFound, string.Empty, message);
    public static ServiceResult Forbidden(string message) => WithError(ServiceStatus.Forbidden, string.Empty, message);
    public static ServiceResult Failed(string message) => WithError(ServiceStatus.Failed, string.Empty, message);

    private static ServiceResult WithError(ServiceStatus status, string field, string message)
    {
        var result = new ServiceResult(status);
        result.Errors.Add(new ServiceError(field, message));
        return result;
    }
}
