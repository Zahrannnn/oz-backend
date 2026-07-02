using Microsoft.AspNetCore.Mvc;

namespace Oz.Api.Controllers;

[ApiController]
[Tags("Health")]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok" });
    }
}
