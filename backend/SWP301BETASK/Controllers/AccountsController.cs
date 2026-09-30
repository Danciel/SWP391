using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP301BETASK.Data;
using SWP301BETASK.Models;

namespace SWP301BETASK.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly LegacyVaultDbContext _context;

        public AccountsController(LegacyVaultDbContext context)
        {
            _context = context;
        }

        // GET: api/accounts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var accounts = await _context.Accounts.ToListAsync();

            return Ok(accounts);
        }

        // POST: api/accounts
        [HttpPost]
        public async Task<IActionResult> Create(Account account)
        {
            account.Id = Guid.NewGuid();
            account.DateCreated= DateTime.UtcNow;

            _context.Accounts.Add(account);

            await _context.SaveChangesAsync();

            return Ok(account);
        }
    }
}