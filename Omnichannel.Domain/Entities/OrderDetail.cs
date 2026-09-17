namespace Omnichannel.Domain.Entities
{
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal LineTotal { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual ProductBatch Batch { get; set; } = null!;
    }
}