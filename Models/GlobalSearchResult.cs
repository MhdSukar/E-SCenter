namespace ESCenter.Models
{
    /// <summary>
    /// One result row shown in the global search autocomplete dropdown.
    /// </summary>
    public class GlobalSearchResult
    {
        /// <summary>The unique identifier shown on the left of the suggestion row.</summary>
        /// <example>ESC-000042, SKU-0001, INV-Battery-iPhone13</example>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>Human-readable description shown between identifier and source.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Module label shown on the right of the suggestion row.</summary>
        /// <example>Repair Tickets, Parts Control, Inventory, Boneyard</example>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Navigation action key used by MainViewModel to navigate and highlight.
        /// Values: "Tickets", "Parts", "Inventory", "Boneyard"
        /// </summary>
        public string NavigationTarget { get; set; } = string.Empty;

        /// <summary>
        /// The raw DB identifier needed to select the item after navigation.
        /// e.g. TicketId (int as string), PartId, InventoryId, DeviceId.
        /// </summary>
        public string ItemId { get; set; } = string.Empty;
    }
}
