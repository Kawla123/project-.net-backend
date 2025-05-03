public class Perfume
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required]
    public int StockQuantity { get; set; }

    [Required]
    public string SupplierId { get; set; }

    [ForeignKey("SupplierId")]
    public virtual AppUser Supplier { get; set; }

    // Navigation property
    public virtual ICollection<Order> Orders { get; set; }
}