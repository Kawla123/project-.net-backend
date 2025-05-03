public class CustomParfum
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public List<CustomParfumComponent> Components { get; set; }
    public Order Order { get; set; }
}
