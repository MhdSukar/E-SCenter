using System;

namespace ESCenter.Models
{
    public class AlBarakaRecord
    {
        public int AlBarakaId { get; set; }
        public DateTime Date { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string PriceCurrency { get; set; } = "S.P";
        public string Category { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }
}
