namespace ESCenter.Models
{
    /// <summary>
    /// A catalog entry shown in the parts autocomplete dropdown.
    /// Carries name, stock level, price, and source so the UI can
    /// show color-coded stock badges without extra lookups.
    /// </summary>
    public class PartSuggestionItem
    {
        public string Name  { get; set; } = string.Empty;
        public string Sku   { get; set; } = string.Empty;
        public int    StockQty { get; set; }
        public double Price    { get; set; }
        public string Source   { get; set; } = string.Empty; // "Parts" | "Inventory"

        // UI helpers
        public bool IsOutOfStock => StockQty <= 0;
        public bool IsLowStock   => StockQty > 0 && StockQty <= 3;

        public string StockDisplay =>
            StockQty <= 0 ? "Out of stock" :
            StockQty <= 3 ? $"{StockQty} low" :
                            $"{StockQty} in stock";
    }
}
