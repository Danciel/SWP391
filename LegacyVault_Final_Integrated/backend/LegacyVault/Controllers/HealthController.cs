using LegacyVault.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly LegacyVaultDbContext _context;

        public HealthController(LegacyVaultDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                status = "ok",
                application = "LegacyVault"
            });
        }

        [HttpGet("database")]
        public async Task<IActionResult> GetDatabaseHealth()
        {
            var connected = await _context.Database.CanConnectAsync();

            if (!connected)
            {
                return Ok(new
                {
                    status = "error",
                    database = "disconnected"
                });
            }

            var accountCount = await _context.Accounts.CountAsync();

            return Ok(new
            {
                status = "ok",
                database = "connected",
                accounts = accountCount
            });
        }

    }
}