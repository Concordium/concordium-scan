using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Rest;

/// <summary>
/// Very simple health end-point for use by the application gateway.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("rest/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public void Get()
    {
    }
}