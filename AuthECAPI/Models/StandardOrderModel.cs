namespace AuthECAPI.Models
{
    public class StandardOrderModel
    {
        public List<OrderItemModel> Items { get; set; }
    }

    public class OrderItemModel
    {
        public int ParfumId { get; set; }
        public int Quantity { get; set; }
    }
}
