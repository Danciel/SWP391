using SWP301BETASK.Features.WillManagement.Contracts;
using SWP301BETASK.Shared.Exceptions;

namespace SWP301BETASK.Features.WillManagement.Services
{
    /// <summary>
    /// TEMPORARY until IdentityAccess ships JWT. Reads the account id from the "X-Debug-User-Id" header
    /// and ONLY works in the Development environment; everywhere else it answers 401.
    /// Replace by registering a JWT-based ICurrentUser (AddWillManagement uses TryAdd, so register yours first).
    /// </summary>
    internal sealed class DevHeaderCurrentUser : ICurrentUser
    {
        public const string HeaderName = "X-Debug-User-Id";

        private readonly IHttpContextAccessor _http;
        private readonly IHostEnvironment _env;

        public DevHeaderCurrentUser(IHttpContextAccessor http, IHostEnvironment env)
        {
            _http = http;
            _env = env;
        }

        public Guid UserId
        {
            get
            {
                if (!_env.IsDevelopment())
                    throw ApiException.Unauthorized("Authentication is not configured yet");

                var raw = _http.HttpContext?.Request.Headers[HeaderName].FirstOrDefault();
                if (!Guid.TryParse(raw, out var id))
                    throw ApiException.Unauthorized($"Missing or invalid {HeaderName} header (Development only)");
                return id;
            }
        }
    }
}
