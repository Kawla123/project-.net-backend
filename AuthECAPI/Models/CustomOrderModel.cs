namespace AuthECAPI.Models
{
    public class CustomOrderModel
    {
        public string Name { get; set; }
        public List<ComponentModel> Components { get; set; }
    }

    public class ComponentModel
    {
        public int ComponentId { get; set; }
        public int Quantity { get; set; }
    }
}
