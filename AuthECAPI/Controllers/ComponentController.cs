using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthECAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthECAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComponentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ComponentController(AppDbContext context)
        {
            _context = context;
        }

        // Endpoint pour récupérer tous les composants disponibles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Component>>> GetComponents()
        {
            var components = await _context.Components
                .Where(c => c.AvailableQuantity > 0)
                .ToListAsync();

            return Ok(components); // Retourne une liste de composants disponibles
        }

        // Endpoint pour récupérer un composant par son ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Component>> GetComponent(int id)
        {
            var component = await _context.Components.FindAsync(id);
            if (component == null)
                return NotFound(); // Si le composant n'existe pas, retour NotFound()

            return Ok(component); // Retourne le composant trouvé
        }

        // Endpoint pour mettre à jour un composant
        [Authorize(Roles = "Supplier")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComponent(int id, Component component)
        {
            if (id != component.Id)
            {
                return BadRequest(); // Retourne une erreur si les IDs ne correspondent pas
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingComponent = await _context.Components.FindAsync(id);

            if (existingComponent == null)
            {
                return NotFound(); // Si le composant n'existe pas, retourne NotFound()
            }

            if (existingComponent.SupplierId != int.Parse(userId))
            {
                return Forbid(); // Si l'utilisateur n'est pas le fournisseur du composant, retourne Forbid()
            }

            existingComponent.Name = component.Name;
            existingComponent.Description = component.Description;
            existingComponent.PricePerUnit = component.PricePerUnit;
            existingComponent.AvailableQuantity = component.AvailableQuantity;

            try
            {
                await _context.SaveChangesAsync(); // Sauvegarde les modifications dans la base de données
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ComponentExists(id))
                {
                    return NotFound(); // Si le composant n'existe pas dans la base de données, retourne NotFound()
                }
                else
                {
                    throw; // Lance une exception si une autre erreur se produit
                }
            }

            return NoContent(); // Retourne NoContent si la mise à jour est réussie
        }

        // Méthode pour vérifier si un composant existe dans la base de données
        private bool ComponentExists(int id)
        {
            return _context.Components.Any(e => e.Id == id);
        }
    }
}
