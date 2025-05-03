using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthECAPI.Models
{
    public class AppDbContext:IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
        { }

        
    

public DbSet<AppUser> AppUsers { get; set; }
public DbSet<Parfum> Parfums { get; set; }
public DbSet<Component> Components { get; set; }
public DbSet<Order> Orders { get; set; }
public DbSet<OrderItem> OrderItems { get; set; }
public DbSet<CustomParfum> CustomParfums { get; set; }
public DbSet<CustomParfumComponent> CustomParfumComponents { get; set; }

protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    // Configure relationships
    builder.Entity<Parfum>()
        .HasOne(p => p.Supplier)
        .WithMany()
        .HasForeignKey(p => p.SupplierId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Entity<Component>()
        .HasOne(c => c.Supplier)
        .WithMany()
        .HasForeignKey(c => c.SupplierId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Entity<Order>()
        .HasOne(o => o.Client)
        .WithMany(c => c.Orders)
        .HasForeignKey(o => o.ClientId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Entity<CustomParfum>()
        .HasOne(cp => cp.Order)
        .WithOne(o => o.CustomParfum)
        .HasForeignKey<CustomParfum>(cp => cp.OrderId);
}
}
}
