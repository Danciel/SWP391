using Microsoft.AspNetCore.Mvc;

namespace SWP301BETASK.Shared.Extensions
{
    public static class ApiServiceExtensions
    {
        /// <summary>Validation failures use the same { message, errors } shape as ApiException (FE reads data.message).</summary>
        public static IMvcBuilder AddStandardApiErrors(this IMvcBuilder builder) =>
            builder.ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = ctx =>
                {
                    var errors = ctx.ModelState
                        .Where(kv => kv.Value is { Errors.Count: > 0 })
                        .SelectMany(kv => kv.Value!.Errors.Select(e => $"{kv.Key}: {e.ErrorMessage}"))
                        .ToList();
                    return new BadRequestObjectResult(new { message = "Validation failed", errors });
                };
            });
    }
}
