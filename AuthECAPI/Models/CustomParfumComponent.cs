public class CustomParfumComponent
{
    public int Id { get; set; }
    public int CustomParfumId { get; set; }
    public int ComponentId { get; set; }
    public int Quantity { get; set; }
    public CustomParfum CustomParfum { get; set; }
    public Component Component { get; set; }
}
