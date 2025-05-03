using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthECAPI.Models;

namespace AuthECAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParfumController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ParfumController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Parfum>>> GetParfums()
        {
            return await _context.Parfums
                .Where(p => p.AvailableQuantity > 0)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Parfum>> GetParfum(int id)
        {
            var parfum = await _context.Parfums.FindAsync(id);
            if (parfum == null)
                return NotFound();

            return Ok(parfum);
        }

        [Authorize(Roles = "Supplier")]
        [HttpPost]
        public async Task<ActionResult<Parfum>> CreateParfum(Parfum parfum)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            parfum.SupplierId = int.Parse(userId);

            _context.Parfums.Add(parfum);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetParfum), new { id = parfum.Id }, parfum);
        }

        [Authorize(Roles = "Supplier")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateParfum(int id, Parfum parfum)
        {
            var existingParfum = await _context.Parfums.FindAsync(id);
            if (existingParfum == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (existingParfum.SupplierId != int.Parse(userId))
                return Forbid();

            existingParfum.Name = parfum.Name;
            existingParfum.Description = parfum.Description;
            existingParfum.Price = parfum.Price;
            existingParfum.AvailableQuantity = parfum.AvailableQuantity;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ParfumExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        private bool ParfumExists(int id)
        {
            return _context.Parfums.Any(e => e.Id == id);
        }
    }
}
