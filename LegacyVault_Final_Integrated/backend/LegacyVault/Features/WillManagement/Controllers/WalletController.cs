using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.WillManagement.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _wallet;

        public WalletController(IWalletService wallet)
        {
            _wallet = wallet;
        }

        // Owner or Executor: balance, held, pending, transactions.
        [HttpGet]
        public async Task<IActionResult> Get()
            => Ok(await _wallet.GetMineAsync());

        // MOCK top-up for demo. No bank is connected.
        [HttpPost("topup")]
        public async Task<IActionResult> TopUp(
            [FromBody] TopUpRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
                return BadRequest(new { message = "Provide a unique Idempotency-Key (max 100 characters) for each top-up attempt." });

            try
            {
                return Ok(await _wallet.TopUpAsync(request.Amount, idempotencyKey.Trim()));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
