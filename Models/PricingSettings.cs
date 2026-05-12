using System.Collections.Generic;

namespace ESCenter.Models
{
    public class PricingSettings
    {
        public bool IncludeDiagnosticFee { get; set; } = true;
        public decimal DiagnosticInspectionFeeSp { get; set; } = 0m;
        public bool WaiveDiagnosticIfRepairProceeds { get; set; } = true;

        public bool IncludeLabourCost { get; set; } = true;
        public decimal TechnicianHourlyRateSp { get; set; } = 0m;
        public decimal DiagnosisHours { get; set; } = 0m;
        public decimal RepairHours { get; set; } = 0m;
        public decimal ComplexityMultiplier { get; set; } = 1m;

        public bool IncludePartsAndComponents { get; set; } = true;
        public decimal DefaultPartsMarkupPercent { get; set; } = 30m;

        public bool IncludeOverheadAllocation { get; set; } = true;
        public decimal MonthlyFixedOverheadSp { get; set; } = 0m;
        public decimal AverageJobsPerMonth { get; set; } = 1m;

        public bool IncludeProfitMargin { get; set; } = true;
        public decimal DesiredProfitMarginPercent { get; set; } = 25m;

        public bool IncludeVatTax { get; set; } = false;
        public decimal VatTaxRatePercent { get; set; } = 0m;

        public List<PricingCostOption> AdditionalCosts { get; set; } = new();
    }
}
