using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthECAPI.Models
{
    // User model (already exists in your code)
    public class AppUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; }

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<CustomOrder> CustomOrders { get; set; }
    }