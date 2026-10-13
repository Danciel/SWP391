namespace SWP301BETASK.Shared.Exceptions
{
    /// <summary>Business error. Serialized by <see cref="ExceptionMiddleware"/> as { message, errors }.</summary>
    public class ApiException : Exception
    {
        public int StatusCode { get; }
        public IReadOnlyList<string>? Errors { get; }

        public ApiException(int statusCode, string message, IEnumerable<string>? errors = null) : base(message)
        {
            StatusCode = statusCode;
            Errors = errors?.ToList();
        }

        public static ApiException BadRequest(string message, IEnumerable<string>? errors = null) => new(400, message, errors);
        public static ApiException Unauthorized(string message) => new(401, message);
        public static ApiException Forbidden(string message) => new(403, message);
        public static ApiException NotFound(string message) => new(404, message);
        public static ApiException Conflict(string message) => new(409, message);
    }
}
