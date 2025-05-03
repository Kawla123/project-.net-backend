using AuthECAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;



namespace AuthECAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Client")]
        [HttpGet("my-orders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Parfum)
                .Include(o => o.CustomParfum)
                    .ThenInclude(cp => cp.Components)
                        .ThenInclude(c => c.Component)
                .Where(o => o.ClientId == userId)
                .ToListAsync();
        }

        [Authorize(Roles = "Supplier")]
        [HttpGet("supplier-orders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetSupplierOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var standardOrders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Parfum)
                .Where(o => o.Type == OrderType.Standard &&
                       o.OrderItems.Any(oi => oi.Parfum.SupplierId == int.Parse(userId)))
                .ToListAsync();

            var customOrders = await _context.Orders
                .Include(o => o.CustomParfum)
                    .ThenInclude(cp => cp.Components)
                        .ThenInclude(c => c.Component)
                .Where(o => o.Type == OrderType.Custom &&
                       o.CustomParfum.Components.Any(c => c.Component.SupplierId == int.Parse(userId)))
                .ToListAsync();

            return standardOrders.Concat(customOrders).ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Parfum)
                .Include(o => o.CustomParfum)
                    .ThenInclude(cp => cp.Components)
                        .ThenInclude(c => c.Component)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Client" && order.ClientId != userId)
            {
                return Forbid();
            }
            else if (userRole == "Supplier")
            {
                bool isSupplierForOrder = false;

                if (order.Type == OrderType.Standard)
                {
                    isSupplierForOrder = order.OrderItems.Any(oi => oi.Parfum.SupplierId == int.Parse(userId));
                }
                else if (order.Type == OrderType.Custom)
                {
                    isSupplierForOrder = order.CustomParfum.Components.Any(c => c.Component.SupplierId == int.Parse(userId));
                }

                if (!isSupplierForOrder)
                {
                    return Forbid();
                }
            }
            else if (userRole != "Admin")
            {
                return Forbid();
            }

            return order;
        }

        [Authorize(Roles = "Client")]
        [HttpPost("create-standard-order")]
        public async Task<ActionResult<Order>> CreateStandardOrder(StandardOrderModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                decimal totalPrice = 0;
                foreach (var item in model.Items)
                {
                    var parfum = await _context.Parfums.FindAsync(item.ParfumId);
                    if (parfum == null)
                    {
                        return BadRequest($"Parfum avec l'ID {item.ParfumId} non trouvé");
                    }

                    if (parfum.AvailableQuantity < item.Quantity)
                    {
                        return BadRequest($"Quantité insuffisante pour le parfum {parfum.Name}");
                    }

                    totalPrice += parfum.Price * item.Quantity;
                    parfum.AvailableQuantity -= item.Quantity;
                }

                var order = new Order
                {
                    ClientId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Status = OrderStatus.Awaiting,
                    Type = OrderType.Standard,
                    TotalPrice = totalPrice,
                    OrderItems = new List<OrderItem>()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in model.Items)
                {
                    var parfum = await _context.Parfums.FindAsync(item.ParfumId);

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ParfumId = item.ParfumId,
                        Quantity = item.Quantity,
                        UnitPrice = parfum.Price,
                        TotalPrice = parfum.Price * item.Quantity
                    };

                    _context.OrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [Authorize(Roles = "Client")]
        [HttpPost("create-custom-order")]
        public async Task<ActionResult<Order>> CreateCustomOrder(CustomOrderModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                decimal totalPrice = 0;

                var customParfum = new CustomParfum
                {
                    Components = new List<CustomParfumComponent>()
                };

                foreach (var item in model.Components)
                {
                    var component = await _context.Components.FindAsync(item.ComponentId);
                    if (component == null)
                    {
                        return BadRequest($"Composant avec l'ID {item.ComponentId} non trouvé");
                    }

                    customParfum.Components.Add(new CustomParfumComponent
                    {
                        ComponentId = item.ComponentId,
                        Quantity = item.Quantity
                    });

                    totalPrice += component.Price * item.Quantity;
                }

                var order = new Order
                {
                    ClientId = userId,
                    CreatedDate = DateTime.UtcNow,
                    Status = OrderStatus.Awaiting,
                    Type = OrderType.Custom,
                    TotalPrice = totalPrice,
                    CustomParfum = customParfum
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
