using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrderController(ApplicationDbContext context)
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

        // Get standard orders where the supplier has parfums
        var standardOrders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Parfum)
            .Where(o => o.Type == OrderType.Standard &&
                   o.OrderItems.Any(oi => oi.Parfum.SupplierId == int.Parse(userId)))
            .ToListAsync();

        // Get custom orders where the supplier has components
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

        // Verify authorization
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
            // Calculate total price and check stock
            decimal totalPrice = 0;
            foreach (var item in model.Items)
            {
                var parfum = await _context.Parfums.FindAsync(item.ParfumId);
                if (parfum == null)
                {
                    return BadRequest($"Parfum with ID {item.ParfumId} not found");
                }

                if (parfum.AvailableQuantity < item.Quantity)
                {
                    return BadRequest($"Insufficient stock for parfum {parfum.Name}");
                }

                totalPrice += parfum.Price * item.Quantity;
                
                // Update stock
                parfum.AvailableQuantity -= item.Quantity;
            }

            // Create order
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

            // Add order items
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
            // Check stock for components
            foreach (var componentItem in model.Components)
            {
                var component = await _context.Components.FindAsync(componentItem.ComponentId);
                if (component == null)
                {
                    return BadRequest($"Component with ID {componentItem.ComponentId} not found");
                }

                if (component.AvailableQuantity < componentItem.Quantity)
                {
                    return BadRequest($"Insufficient stock for component {component.Name}");
                }
                
                // Update stock
                component.AvailableQuantity -= componentItem.Quantity;
            }

            // Create order (price will be set by supplier later)
            var order = new Order
            {
                ClientId = userId,
                CreatedDate = DateTime.UtcNow,
                Status = OrderStatus.Awaiting,
                Type = OrderType.Custom,
                TotalPrice = 0, // Will be set by supplier
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create custom parfum
            var customParfum = new CustomParfum
            {
                OrderId = order.Id,
                Name = model.Name,
                Price = 0, // Will be set by supplier
                Components = new List<CustomParfumComponent>()
            };

            _context.CustomParfums.Add(customParfum);
            await _context.SaveChangesAsync();

            // Add components
            foreach (var componentItem in model.Components)
            {
                var customParfumComponent = new CustomParfumComponent
                {
                    CustomParfumId = customParfum.Id,
                    ComponentId = componentItem.ComponentId,
                    Quantity = componentItem.Quantity
                };

                _context.CustomParfumComponents.Add(customParfumComponent);
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

    [Authorize(Roles = "Supplier")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatusUpdateModel model)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound();
        }

        // Verify supplier has rights to this order
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool hasRights = false;

        if (order.Type == OrderType.Standard)
        {
            hasRights = await _context.OrderItems
                .Where(oi => oi.OrderId == id)
                .AnyAsync(oi => oi.Parfum.SupplierId == int.Parse(userId));
        }
        else if (order.Type == OrderType.Custom)
        {
            hasRights = await _context.CustomParfumComponents
                .Include(cpc => cpc.Component)
                .Where(cpc => cpc.CustomParfum.OrderId == id)
                .AnyAsync(cpc => cpc.Component.SupplierId == int.Parse(userId));
        }

        if (!hasRights)
        {
            return Forbid();
        }

        // Update status
        order.Status = model.Status;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Supplier")]
    [HttpPut("custom/{id}/price")]
    public async Task<IActionResult> SetCustomParfumPrice(int id, CustomPriceModel model)
    {
        var customParfum = await _context.CustomParfums
            .Include(cp => cp.Order)
            .Include(cp => cp.Components)
                .ThenInclude(c => c.Component)
            .FirstOrDefaultAsync(cp => cp.Id == id);

        if (customParfum == null)
        {
            return NotFound();
        }

        // Verify supplier has rights to this custom parfum
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool hasRights = customParfum.Components
            .Any(c => c.Component.SupplierId == int.Parse(userId));

        if (!hasRights)
        {
            return Forbid();
        }

        // Set price
        customParfum.Price = model.Price;
        customParfum.Order.TotalPrice = model.Price;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

