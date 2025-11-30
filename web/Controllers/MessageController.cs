using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    [HttpGet]
    public IActionResult GetMessage()
    {
        return Ok(new { message = "Hello Hono!" });
    }
}

