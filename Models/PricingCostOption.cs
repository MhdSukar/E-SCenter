namespace ESCenter.Models
{
    public class PricingCostOption
    {
        public string Label { get; set; } = string.Empty;
        public bool IsIncluded { get; set; }
        public decimal Value { get; set; }
        public string ValueType { get; set; } = "Fixed"; // Fixed | Percent
    }
}
