using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.IdentityAccess.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        // ==========================================
        // PHONE OTP ENDPOINTS
        // ==========================================

        [HttpPost("send-phone-otp")]
        [Authorize] // <-- The user must be logged in to send an OTP to their own phone
        public async Task<IActionResult> SendPhoneOtp()
        {
            try
            {
                var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(accountIdString, out var accountId))
                    return Unauthorized(new { message = "Invalid token." });

                await _authService.SendPhoneOtpAsync(accountId);
                return Ok(new { message = "OTP sent successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-phone-otp")]
        [Authorize]
        public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyPhoneOtpRequest request)
        {
            try
            {
                var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(accountIdString, out var accountId))
                    return Unauthorized(new { message = "Invalid token." });

                await _authService.VerifyPhoneOtpAsync(accountId, request.Otp);
                return Ok(new { message = "Phone verified successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                await _authService.RequestPasswordResetAsync(request.Email);

                return Ok(new { message = "If an account with that email exists, an OTP has been sent." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request);

                return Ok(new { message = "Password has been reset successfully. You can now log in." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}