public class CustomOrderComponent
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CustomOrderId { get; set; }

    [Required]
    public int ComponentId { get; set; }

    [Required]
    public int Quantity { get; set; }

    // Navigation properties
    [ForeignKey("CustomOrderId")]
    public virtual CustomOrder CustomOrder { get; set; }

    [ForeignKey("ComponentId")]
    public virtual Component Component { get; set; }
}