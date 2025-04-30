using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthECAPI.Controllers
{
    [Authorize(Roles = "Client")]
    [ApiController]
    [Route("api/[controller]")]
    public class ClientController : ControllerBase
    {
        [HttpGet("data")]
        public IActionResult GetClientData()
        {
            return Ok("Données visibles uniquement par les clients.");
        }
    }
}
