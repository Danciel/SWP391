using SWP301BETASK.Data;
using SWP301BETASK.Models;
using SWP301BETASK.Features.Admin.DTOs;
using SWP301BETASK.Features.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SWP301BETASK.Features.Admin.Controllers;

[ApiController]
[Route("api/identity")]
[Authorize]
[EnableRateLimiting("auth")]
public class IdentityController(
    LegacyVaultDbContext db,
    ICurrentUser currentUser,
    IOtpService otp,
    IIdentityProviderClient provider) : ControllerBase
{
    /// <summary>Sends an SMS OTP to the phone number on the account (extra endpoint; pairs with verify-phone).</summary>
    [HttpPost("send-phone-otp")]
    public async Task<ActionResult<SendOtpResponse>> SendPhoneOtp(CancellationToken ct)
    {
        var account = await currentUser.GetAccountAsync(db, ct);
        if (account is null) return Unauthorized();
        if (account.PhoneVerified)
            return Problem(detail: "Phone is already verified.", statusCode: StatusCodes.Status409Conflict);

        var result = await otp.SendAsync(account, OtpPurpose.Phone, ct);
        return result.Success
            ? Ok(new SendOtpResponse("Verification code sent by SMS.", result.DevCode))
            : Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("verify-phone")]
    public async Task<ActionResult<AccountResponse>> VerifyPhone(OtpCodeRequest req, CancellationToken ct)
    {
        var account = await currentUser.GetAccountAsync(db, ct);
        if (account is null) return Unauthorized();

        var result = await otp.VerifyAsync(account, OtpPurpose.Phone, req.Code, ct);
        if (!result.Success)
            return Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);

        account.PhoneVerified = true;
        await db.SaveChangesAsync(ct);
        return Ok(account.ToResponse());
    }

    /// <summary>Submits the user to the (mock) third-party identity provider. Requires a verified phone.</summary>
    [HttpPost("verify")]
    public async Task<ActionResult<IdentityStatusResponse>> Verify(IdentityVerifyRequest req, CancellationToken ct)
    {
        var account = await currentUser.GetAccountAsync(db, ct);
        if (account is null) return Unauthorized();

        if (!account.PhoneVerified)
            return Problem(detail: "Verify your phone number before identity verification.", statusCode: StatusCodes.Status400BadRequest);

        if (account.IdentityStatus == IdentityStatus.VERIFIED)
            return Problem(detail: "Identity is already verified.", statusCode: StatusCodes.Status409Conflict);

        account.IdentityStatus = IdentityStatus.PENDING;
        await db.SaveChangesAsync(ct);

        var result = await provider.VerifyAsync(
            new IdentityCheckRequest(req.FullName.Trim(), req.DocumentNumber.Trim(), req.DateOfBirth!.Value), ct);

        if (result.Verified)
        {
            account.IdentityStatus = IdentityStatus.VERIFIED;
            account.ExternalIdentityId = result.ExternalIdentityId;
            account.FullName = req.FullName.Trim();
        }
        else
        {
            account.IdentityStatus = IdentityStatus.REJECTED;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new IdentityStatusResponse(account.IdentityStatus, account.ExternalIdentityId, result.Reason));
    }
}
