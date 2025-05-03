public class Component
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal PricePerUnit { get; set; }
    public int AvailableQuantity { get; set; }
    public List<CustomParfumComponent> CustomParfumComponents { get; set; }
}
