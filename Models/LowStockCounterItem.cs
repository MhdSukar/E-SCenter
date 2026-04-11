namespace ESCenter.Models
{
    public class LowStockCounterItem
    {
        public string Source { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
