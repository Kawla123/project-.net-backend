public class Parfum
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
    public int SupplierId { get; set; }
    public List<OrderItem> OrderItems { get; set; }
}
