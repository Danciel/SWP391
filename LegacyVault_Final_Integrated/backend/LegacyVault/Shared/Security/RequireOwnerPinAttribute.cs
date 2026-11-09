
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LegacyVault.Shared.Security;

// Gan attribute vao API can xac minh PIN
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireOwnerPinAttribute : TypeFilterAttribute
{
    public RequireOwnerPinAttribute(string scope)
        : base(typeof(RequireOwnerPinFilter))
    {
        Arguments = new object[] { scope };
    }
}

// Kiem tra PIN Grant truoc khi Controller xu ly
public sealed class RequireOwnerPinFilter : IAsyncActionFilter
{
    private readonly string _scope;
    private readonly ICurrentUserService _currentUser;
    private readonly OwnerPinGrantService _grantService;

    public RequireOwnerPinFilter(
        string scope,
        ICurrentUserService currentUser,
        OwnerPinGrantService grantService)
    {
        _scope = scope;
        _currentUser = currentUser;
        _grantService = grantService;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // B1. Lay Asset ID hoac Will ID tu route
        string? rawId =
            context.RouteData.Values["id"]?.ToString();

        if (!Guid.TryParse(rawId, out Guid resourceId))
        {
            context.Result = new BadRequestObjectResult(
                new { message = "Invalid resource ID." }
            );
            return;
        }

        // B2. Lay Owner hien tai tu JWT
        Guid ownerId =
            _currentUser.GetRequiredAccountId();

        // B3. Lay Grant do Backend cap sau khi PIN dung
        string grant = context.HttpContext.Request.Headers[
            "X-Owner-Pin-Grant"
        ].ToString();

        // B4. Kiem tra Grant
        bool valid = _grantService.IsValidGrant(
            grant,
            ownerId,
            _scope,
            resourceId
        );

        if (!valid)
        {
            context.Result = new ObjectResult(new
            {
                message =
                    "Please verify your 6-digit security PIN."
            })
            {
                StatusCode = 423
            };
            return;
        }

        // B5. Grant hop le - tiep tuc vao Controller
        // Controller van phai kiem tra quyen so huu.
        await next();
    }
}
