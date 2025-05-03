public class Order
{
    public int Id { get; set; }
    public string ClientId { get; set; }
    public DateTime CreatedDate { get; set; }
    public OrderStatus Status { get; set; }
    public OrderType Type { get; set; }
    public decimal TotalPrice { get; set; }
    public List<OrderItem> OrderItems { get; set; }
    public CustomParfum CustomParfum { get; set; }
}
