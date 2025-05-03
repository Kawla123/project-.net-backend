public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ClientId { get; set; }

    [Required]
    public int PerfumeId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    [Required]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // Navigation properties
    [ForeignKey("ClientId")]
    public virtual AppUser Client { get; set; }

    [ForeignKey("PerfumeId")]
    public virtual Perfume Perfume { get; set; }
}