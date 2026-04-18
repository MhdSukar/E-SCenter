namespace ESCenter.Models
{
    public class BoneyardModel
    {
        private static readonly string[] DeviceTypeLabels =
        {
            "Phone", "Tablet", "Laptop", "Other"
        };

        public int DeviceId { get; set; }
        public int DeviceType { get; set; } = -1;
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Condition { get; set; }
        public string HolderID { get; set; }
        public string Notes { get; set; }
        public double Price { get; set; }
        public string PriceCurrency { get; set; } = "S.P";
        public string AddedAt { get; set; }

        public string DeviceTypeText =>
            DeviceType >= 0 && DeviceType < DeviceTypeLabels.Length
                ? DeviceTypeLabels[DeviceType]
                : "Unknown";
    }
}
