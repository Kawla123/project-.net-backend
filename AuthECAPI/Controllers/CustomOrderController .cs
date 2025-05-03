using AuthECAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthECAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomOrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomOrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/CustomOrder/client
        [HttpGet("client")]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult<IEnumerable<CustomOrder>>> GetClientCustomOrders()
        {
            var userId = User.FindFirstValue("UserID");
            return await _context.CustomOrders
                .Include(co => co.Components)
                .ThenInclude(coc => coc.Component)
                .Where(co => co.ClientId == userId)
                .ToListAsync();
        }

        // GET: api/CustomOrder/supplier
        [HttpGet("supplier")]
        [Authorize(Roles = "Supplier")]
        public async Task<ActionResult<IEnumerable<CustomOrder>>> GetSupplierCustomOrders()
        {
            // For custom orders, all suppliers can see them
            return await _context.CustomOrders
                .Include(co => co.Client)
                .Include(co => co.Components)
                .ThenInclude(coc => coc.Component)
                .ToListAsync();
        }

        // GET: api/CustomOrder/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<CustomOrder>> GetCustomOrder(int id)
        {
            var customOrder = await _context.CustomOrders
                .Include(co => co.Components)
                .ThenInclude(coc => coc.Component)
                .FirstOrDefaultAsync(co => co.Id == id);

            if (customOrder == null)
            {
                return NotFound();
            }

            // Check if the user has access to this order
            var userId = User.FindFirstValue("UserID");
            bool isAdmin = User.IsInRole("Admin");
            bool isClient = User.IsInRole("Client") && customOrder.ClientId == userId;
            bool isSupplier = User.IsInRole("Supplier"); // All suppliers can access custom orders

            if (!isAdmin && !isClient && !isSupplier)
            {
                return Unauthorized();
            }

            return customOrder;
        }

        // POST: api/CustomOrder
        [HttpPost]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult<CustomOrder>> CreateCustomOrder(CustomOrderRequestModel customOrderRequest)
        {
            var userId = User.FindFirstValue("UserID");

            if (customOrderRequest.Components == null || !customOrderRequest.Components.Any())
            {
                return BadRequest("No components specified");
            }

            // Validate components and calculate total price
            decimal totalPrice = 0;
            var componentIds = customOrderRequest.Components.Select(c => c.ComponentId).ToList();
            var components = await _context.Components.Where(c => componentIds.Contains(c.Id)).ToListAsync();

            if (components.Count != componentIds.Count)
            {
                return BadRequest("One or more components not found");
            }

            // Check stock and calculate price
            foreach (var requestComponent in customOrderRequest.Components)
            {
                var component = components.First(c => c.Id == requestComponent.ComponentId);
                if (component.StockQuantity < requestComponent.Quantity)
                {
                    return BadRequest($"Not enough stock for component {component.Name}");
                }

                totalPrice += component.PricePerUnit * requestComponent.Quantity;
            }

            // Create custom order
            var customOrder = new CustomOrder
            {
                ClientId = userId,
                TotalPrice = totalPrice,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                Components = new List<CustomOrderComponent>()
            };

            _context.CustomOrders.Add(customOrder);
            await _context.SaveChangesAsync();

            // Add components and update stock
            foreach (var requestComponent in customOrderRequest.Components)
            {
                var component = components.First(c => c.Id == requestComponent.ComponentId);

                var customOrderComponent = new CustomOrderComponent
                {
                    CustomOrderId = customOrder.Id,
                    ComponentId = requestComponent.ComponentId,
                    Quantity = requestComponent.Quantity
                };

                _context.CustomOrderComponents.Add(customOrderComponent);

                // Update stock
                component.StockQuantity -= requestComponent.Quantity;
                _context.Entry(component).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCustomOrder", new { id = customOrder.Id }, customOrder);
        }

        // PUT: api/CustomOrder/5/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Supplier,Admin")]
        public async Task<IActionResult> UpdateCustomOrderStatus(int id, [FromBody] OrderStatus status)
        {
            var customOrder = await _context.CustomOrders.FindAsync(id);

            if (customOrder == null)
            {
                return NotFound();
            }

            customOrder.Status = status;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/CustomOrder/5/price
        [HttpPut("{id}/price")]
        [Authorize(Roles = "Supplier")]
        public async Task<IActionResult> UpdateCustomOrderPrice(int id, [FromBody] decimal price)
        {
            var customOrder = await _context.CustomOrders.FindAsync(id);

            if (customOrder == null)
            {
                return NotFound();
            }

            customOrder.TotalPrice = price;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/CustomOrder/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCustomOrder(int id)
        {
            var customOrder = await _context.CustomOrders.FindAsync(id);
            if (customOrder == null)
            {
                return NotFound();
            }

            _context.CustomOrders.Remove(customOrder);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}