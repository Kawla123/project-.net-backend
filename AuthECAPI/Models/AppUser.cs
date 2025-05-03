using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using AuthECAPI.Controllers;
using AuthECAPI.Extensions;
using AuthECAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthECAPI.Models
{
    public class AppUser : IdentityUser
    {
        [PersonalData]
        [Column(TypeName = "nvarchar(150)")]
        

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public UserRole Role { get; set; }
        public virtual ICollection<Order> Orders { get; set; }


    }
    public enum UserRole
    {
        Admin,
        Supplier,
        Client
    }

    // Product models
    public class Parfum
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int AvailableQuantity { get; set; }
        public int SupplierId { get; set; }
        public virtual AppUser Supplier { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public string ImageUrl { get; set; }
    }

    public class Component
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PricePerUnit { get; set; }
        public int AvailableQuantity { get; set; }
        public int SupplierId { get; set; }
        public virtual AppUser Supplier { get; set; }
        public virtual ICollection<CustomParfumComponent> CustomParfumComponents { get; set; }
    }

    // Order models
    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public OrderStatus Status { get; set; }
        public string ClientId { get; set; }
        public virtual AppUser Client { get; set; }
        public OrderType Type { get; set; } // Standard or Custom
        public decimal TotalPrice { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual CustomParfum CustomParfum { get; set; }
    }

    public enum OrderStatus
    {
        Awaiting,
        Production,
        Delivered
    }

    public enum OrderType
    {
        Standard,
        Custom
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }
        public int ParfumId { get; set; }
        public virtual Parfum Parfum { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CustomParfum
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; } // Set by supplier
        public virtual ICollection<CustomParfumComponent> Components { get; set; }
    }

    public class CustomParfumComponent
    {
        public int Id { get; set; }
        public int CustomParfumId { get; set; }
        public virtual CustomParfum CustomParfum { get; set; }
        public int ComponentId { get; set; }
        public virtual Component Component { get; set; }
        public int Quantity { get; set; }
    }
}