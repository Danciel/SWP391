using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.IdentityAccess.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.IdentityAccess.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            try
            {
                var account =
                    await _authService.RegisterAsync(request);

                return StatusCode(201, new
                {
                    message = "Account created successfully.",
                    user = account
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(
    [FromBody] VerifyEmailOtpRequest request)
        {
            try
            {
                await _authService.VerifyEmailOtpAsync(request);

                return Ok(new
                {
                    message = "Email verified successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp(
    [FromBody] ResendEmailOtpRequest request)
        {
            try
            {
                await _authService.ResendEmailOtpAsync(request);

                return Ok(new
                {
                    message = "OTP resent successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(
    [FromBody] LoginRequest request)
        {
            try
            {
                var result =
                    await _authService.LoginAsync(request);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}