namespace MockIdpApi.Features.IdentityAccess.Contracts
{
    public enum ServiceStatus
    {
        Ok,
        Created,
        Invalid,
        NotFound,
        Conflict,
        Forbidden
    }

    public class ServiceResult<T>
    {
        public ServiceStatus Status { get; private init; }
        public T? Data { get; private init; }
        public string? Error { get; private init; }

        public static ServiceResult<T> Ok(T data) =>
            new() { Status = ServiceStatus.Ok, Data = data };

        public static ServiceResult<T> Created(T data) =>
            new() { Status = ServiceStatus.Created, Data = data };

        public static ServiceResult<T> Fail(ServiceStatus status, string error) =>
            new() { Status = status, Error = error };
    }
}
