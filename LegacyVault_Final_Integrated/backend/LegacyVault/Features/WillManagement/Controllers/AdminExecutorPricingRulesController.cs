using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.WillManagement.Controllers;

// Admin edits the weights behind the suggested Executor fee.
[ApiController]
[Route("api/admin/executor-pricing-rules")]
[Authorize(Roles = "ADMIN")]
public class AdminExecutorPricingRulesController : ControllerBase
{
    private readonly IExecutorPricingService _pricing;
    private readonly ICurrentUserService _user;

    public AdminExecutorPricingRulesController(IExecutorPricingService pricing, ICurrentUserService user)
    {
        _pricing = pricing;
        _user = user;
    }

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _pricing.GetRulesResponseAsync());

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateExecutorPricingRuleRequest request)
    {
        try { return Ok(await _pricing.UpdateRulesAsync(_user.GetRequiredAccountId(), request)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
