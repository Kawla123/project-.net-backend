using AuthECAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthECAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Order/client
        [HttpGet("client")]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult<IEnumerable<Order>>> GetClientOrders()
        {
            var userId = User.FindFirstValue("UserID");
            return await _context.Orders
                .Include(o => o.Perfume)
                .Where(o => o.ClientId == userId)
                .ToListAsync();
        }

        // GET: api/Order/supplier
        [HttpGet("supplier")]
        [Authorize(Roles = "Supplier")]
        public async Task<ActionResult<IEnumerable<Order>>> GetSupplierOrders()
        {
            var userId = User.FindFirstValue("UserID");
            return await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Perfume)
                .Where(o => o.Perfume.SupplierId == userId)
                .ToListAsync();
        }

        // GET: api/Order/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Perfume)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Check if the user has access to this order
            var userId = User.FindFirstValue("UserID");
            bool isAdmin = User.IsInRole("Admin");
            bool isClient = User.IsInRole("Client") && order.ClientId == userId;
            bool isSupplier = User.IsInRole("Supplier") && order.Perfume.SupplierId == userId;

            if (!isAdmin && !isClient && !isSupplier)
            {
                return Unauthorized();
            }

            return order;
        }

        // POST: api/Order
        [HttpPost]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult<Order>> CreateOrder(OrderRequestModel orderRequest)
        {
            var userId = User.FindFirstValue("UserID");
            var perfume = await _context.Perfumes.FindAsync(orderRequest.PerfumeId);

            if (perfume == null)
            {
                return BadRequest("Perfume not found");
            }

            if (perfume.StockQuantity < orderRequest.Quantity)
            {
                return BadRequest("Not enough stock available");
            }

            var order = new Order
            {
                ClientId = userId,
                PerfumeId = orderRequest.PerfumeId,
                Quantity = orderRequest.Quantity,
                TotalPrice = perfume.Price * orderRequest.Quantity,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending
            };

            // Update stock quantity
            perfume.StockQuantity -= orderRequest.Quantity;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOrder", new { id = order.Id }, order);
        }

        // PUT: api/Order/5/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Supplier,Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatus status)
        {
            var order = await _context.Orders
                .Include(o => o.Perfume)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // For suppliers, check if they own the perfume
            if (User.IsInRole("Supplier"))
            {
                var userId = User.FindFirstValue("UserID");
                if (order.Perfume.SupplierId != userId)
                {
                    return Unauthorized();
                }
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Order/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}