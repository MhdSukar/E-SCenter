namespace ESCenter.Models
{
    public class TicketTemplate
    {
        public int TemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DeviceCategory { get; set; } = string.Empty;
        public string DeviceBrand { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string ProblemDescription { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string PriorityLevel { get; set; } = "Normal";
        public decimal? EstimatedCost { get; set; }
        public string EstimatedCostCurrency { get; set; } = "S.P";
        public string PartsUsed { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}
