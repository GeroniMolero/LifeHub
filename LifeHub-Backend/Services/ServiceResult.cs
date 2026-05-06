namespace LifeHub.Services
{
    public enum ServiceResultStatus { Ok, NotFound, Forbidden, BadRequest, Conflict, Unauthorized }

    public class ServiceResult<T>
    {
        public bool IsSuccess => Status == ServiceResultStatus.Ok;
        public T? Value { get; private init; }
        public string? ErrorMessage { get; private init; }
        public ServiceResultStatus Status { get; private init; }

        public static ServiceResult<T> Ok(T value) =>
            new() { Status = ServiceResultStatus.Ok, Value = value };

        public static ServiceResult<T> NotFound(string message) =>
            new() { Status = ServiceResultStatus.NotFound, ErrorMessage = message };

        public static ServiceResult<T> Forbidden(string message) =>
            new() { Status = ServiceResultStatus.Forbidden, ErrorMessage = message };

        public static ServiceResult<T> BadRequest(string message) =>
            new() { Status = ServiceResultStatus.BadRequest, ErrorMessage = message };

        public static ServiceResult<T> Conflict(string message) =>
            new() { Status = ServiceResultStatus.Conflict, ErrorMessage = message };

        public static ServiceResult<T> Unauthorized(string message) =>
            new() { Status = ServiceResultStatus.Unauthorized, ErrorMessage = message };
    }
}
