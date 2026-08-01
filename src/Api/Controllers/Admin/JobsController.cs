using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oz.Api.Jobs;

namespace Oz.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Tags("Admin - Jobs")]
[Route("api/v1/admin/jobs")]
public class JobsController : ControllerBase
{
    private readonly AutoCancelOrdersJob _autoCancelJob;

    public JobsController(AutoCancelOrdersJob autoCancelJob)
    {
        _autoCancelJob = autoCancelJob;
    }

    [HttpPost("run-auto-cancel")]
    public async Task<IActionResult> RunAutoCancel()
    {
        await _autoCancelJob.ExecuteCoreAsync();
        return Ok(new { message = "Auto-cancel job completed" });
    }
}
