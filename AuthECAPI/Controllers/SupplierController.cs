using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthECAPI.Controllers
{
    [Authorize(Roles = "Supplier")]
    [ApiController]
    [Route("api/[controller]")]
    public class SupplierController : ControllerBase
    {
        [HttpGet("data")]
        public IActionResult GetSupplierData()
        {
            return Ok("Données visibles uniquement par les fournisseurs.");
        }
    }
}
