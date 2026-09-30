using Microsoft.AspNetCore.Mvc;

namespace SWP301BETASK.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok",
                service = "LegacyVault API",
                time = DateTime.UtcNow
            });
        }
    }
}
