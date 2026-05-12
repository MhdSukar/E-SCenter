using System;
using ESCenter.Core;

namespace ESCenter.Models
{
    /// <summary>
    /// Represents one line in a ticket's "Parts Used" list.
    /// Serialized as a JSON array of compact DTOs inside the PartsUsed text column.
    /// </summary>
    public class UsedPartLine : ObservableObject
    {
        // ── Persisted fields ────────────────────────────────────────────────
        public string Name      { get; set; } = string.Empty;
        public string Sku       { get; set; } = string.Empty;   // empty for inventory / custom parts
        public double UnitPrice { get; set; }
        public string PriceCurrency { get; set; } = "S.P";
        public bool   IsCustom  { get; set; }                   // true = typed by user, not in catalog

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, Math.Max(1, value));
        }

        // ── Session-only state (not serialized) ─────────────────────────────
        /// <summary>
        /// How many units we deducted from stock in this editing session.
        /// Used to correctly restock when a part is decremented or removed.
        /// </summary>
        public int DeductedQtyInSession { get; set; }

        // ── UI helper flags (populated from catalog on load) ─────────────────
        public bool IsOutOfStock { get; set; }
        public bool IsLowStock   { get; set; }
    }
}
