using AuthECAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthECAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PerfumeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public PerfumeController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Perfume
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Perfume>>> GetPerfumes()
        {
            return await _context.Perfumes.ToListAsync();
        }

        // GET: api/Perfume/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Perfume>> GetPerfume(int id)
        {
            var perfume = await _context.Perfumes.FindAsync(id);

            if (perfume == null)
            {
                return NotFound();
            }

            return perfume;
        }

        // POST: api/Perfume
        [HttpPost]
        [Authorize(Roles = "Supplier")]
        public async Task<ActionResult<Perfume>> CreatePerfume(Perfume perfume)
        {
            // Get current user ID
            var userId = User.FindFirstValue("UserID");

            // Set the supplier ID to the current user
            perfume.SupplierId = userId;

            _context.Perfumes.Add(perfume);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPerfume", new { id = perfume.Id }, perfume);
        }

        // PUT: api/Perfume/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> UpdatePerfume(int id, Perfume perfume)
        {
            if (id != perfume.Id)
            {
                return BadRequest();
            }

            // Get current user ID
            var userId = User.FindFirstValue("UserID");

            // Check if the perfume belongs to the current supplier
            var existingPerfume = await _context.Perfumes.FindAsync(id);
            if (existingPerfume == null || existingPerfume.SupplierId != userId)
            {
                return Unauthorized();
            }

            // Keep the original supplier ID
            perfume.SupplierId = existingPerfume.SupplierId;

            _context.Entry(existingPerfume).State = EntityState.Detached;
            _context.Entry(perfume).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PerfumeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Perfume/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Supplier,Admin")]
        public async Task<IActionResult> DeletePerfume(int id)
        {
            var perfume = await _context.Perfumes.FindAsync(id);
            if (perfume == null)
            {
                return NotFound();
            }

            // For suppliers, check if the perfume belongs to them
            if (User.IsInRole("Supplier"))
            {
                var userId = User.FindFirstValue("UserID");
                if (perfume.SupplierId != userId)
                {
                    return Unauthorized();
                }
            }

            _context.Perfumes.Remove(perfume);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Perfume/supplier
        [HttpGet("supplier")]
        [Authorize(Roles = "Supplier")]
        public async Task<ActionResult<IEnumerable<Perfume>>> GetSupplierPerfumes()
        {
            var userId = User.FindFirstValue("UserID");
            return await _context.Perfumes
                .Where(p => p.SupplierId == userId)
                .ToListAsync();
        }

        private bool PerfumeExists(int id)
        {
            return _context.Perfumes.Any(e => e.Id == id);
        }
    }
}