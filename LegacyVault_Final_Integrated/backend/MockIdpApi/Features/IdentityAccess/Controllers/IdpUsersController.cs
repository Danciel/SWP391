using MockIdpApi.Features.IdentityAccess.Contracts;
using MockIdpApi.Features.IdentityAccess.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MockIdpApi.Features.IdentityAccess.Controllers
{
    [ApiController]
    [Route("api/idp-users")]
//    [Authorize/*(Roles = AdminRole)*/]
    public class IdpUsersController : ControllerBase
    {
        private const string AdminRole = "ADMIN";

        private readonly IIdpUserService _idpUserService;

        public IdpUsersController(IIdpUserService idpUserService)
        {
            _idpUserService = idpUserService;
        }

        // =========================
        // LIST
        // GET api/idp-users?page=1&pageSize=20&search=...&identityStatus=VERIFIED
        // =========================
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? identityStatus = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _idpUserService.ListAsync(
                page, pageSize, search, identityStatus, cancellationToken);

            return ToActionResult(result);
        }

        // =========================
        // SEARCH (by email XOR phone)
        // GET api/idp-users/search?email=... | ?phone=...
        // =========================
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? email,
            [FromQuery] string? phone,
            CancellationToken cancellationToken)
        {
            var result = await _idpUserService.SearchAsync(
                email, phone, cancellationToken);

            return ToActionResult(result);
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _idpUserService.GetByIdAsync(id, cancellationToken);

            return ToActionResult(result);
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] AdminCreateIdpUserRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _idpUserService.CreateAsync(request, cancellationToken);

            if (result.Status == ServiceStatus.Created)
            {
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Data!.Id },
                    result.Data);
            }

            return ToActionResult(result);
        }

        // =========================
        // UPDATE (partial)
        // =========================
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] AdminUpdateIdpUserRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _idpUserService.UpdateAsync(
                id, request, cancellationToken);

            return ToActionResult(result);
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _idpUserService.DeleteAsync(id, cancellationToken);

            return result.Status == ServiceStatus.Ok
                ? NoContent()
                : ToActionResult(result);
        }

        // =========================
        // HELPERS
        // =========================
        private IActionResult ToActionResult<T>(ServiceResult<T> result)
        {
            return result.Status switch
            {
                ServiceStatus.Ok => Ok(result.Data),
                ServiceStatus.Created => StatusCode(StatusCodes.Status201Created, result.Data),
                ServiceStatus.NotFound => NotFound(new { message = result.Error }),
                ServiceStatus.Conflict => Conflict(new { message = result.Error }),
                ServiceStatus.Forbidden => StatusCode(
                    StatusCodes.Status403Forbidden, new { message = result.Error }),
                _ => BadRequest(new { message = result.Error })
            };
        }
    }
}