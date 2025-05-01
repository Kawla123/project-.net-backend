using System;

[Route("api/[controller]")]
[ApiController]
public class ComponentController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ComponentController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Component>>> GetComponents()
    {
        return await _context.Components
            .Where(c => c.AvailableQuantity > 0)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Component>> GetComponent(int id)
    {
        var component = await _context.Components.FindAsync(id);

        if (component == null)
        {
            return NotFound();
        }

        return component;
    }

    [Authorize(Roles = "Supplier")]
    [HttpPost]
    public async Task<ActionResult<Component>> CreateComponent(Component component)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        component.SupplierId = int.Parse(userId);

        _context.Components.Add(component);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetComponent), new { id = component.Id }, component);
    }

    [Authorize(Roles = "Supplier")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateComponent(int id, Component component)
    {
        if (id != component.Id)
        {
            return BadRequest();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existingComponent = await _context.Components.FindAsync(id);

        if (existingComponent == null)
        {
            return NotFound();
        }

        if (existingComponent.SupplierId != int.Parse(userId))
        {
            return Forbid();
        }

        existingComponent.Name = component.Name;
        existingComponent.Description = component.Description;
        existingComponent.PricePerUnit = component.PricePerUnit;
        existingComponent.AvailableQuantity = component.AvailableQuantity;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ComponentExists(id))
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

    private bool ComponentExists(int id)
    {
        return _context.Components.Any(e => e.Id == id);
    }
}