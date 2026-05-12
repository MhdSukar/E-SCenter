using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ESCenter.Models;

namespace ESCenter.Services
{
    public sealed class PricingBreakdownLine
    {
        public string Component { get; set; } = string.Empty;
        public decimal AmountSp { get; set; }
        public decimal PercentOfPreTax { get; set; }
    }

    public sealed class PricingCalculationResult
    {
        public decimal DiagnosticFeeSp { get; set; }
        public decimal LabourCostSp { get; set; }
        public decimal PartsCostSp { get; set; }
        public decimal PartsMarkupSp { get; set; }
        public decimal PartsSubtotalSp { get; set; }
        public decimal OverheadPerJobSp { get; set; }
        public decimal AdditionalCostsSp { get; set; }
        public decimal TotalDirectCostSp { get; set; }
        public decimal ProfitAmountSp { get; set; }
        public decimal PreTaxRepairPriceSp { get; set; }
        public decimal VatAmountSp { get; set; }
        public decimal FinalCostSp { get; set; }
        public List<string> Lines { get; } = new();
        public List<PricingBreakdownLine> Breakdown { get; } = new();
    }

    public static class PricingService
    {
        public const string FixedValueType = "Fixed";
        public const string PercentValueType = "Percent";

        public static PricingSettings CreateDefaultSettings() => new()
        {
            IncludeDiagnosticFee = true,
            DiagnosticInspectionFeeSp = 0m,
            WaiveDiagnosticIfRepairProceeds = true,
            IncludeLabourCost = true,
            TechnicianHourlyRateSp = 0m,
            DiagnosisHours = 0m,
            RepairHours = 0m,
            ComplexityMultiplier = 1m,
            IncludePartsAndComponents = true,
            DefaultPartsMarkupPercent = 30m,
            IncludeOverheadAllocation = true,
            MonthlyFixedOverheadSp = 0m,
            AverageJobsPerMonth = 1m,
            IncludeProfitMargin = true,
            DesiredProfitMarginPercent = 25m,
            IncludeVatTax = false,
            VatTaxRatePercent = 0m,
            AdditionalCosts = CreateDefaultOptions()
        };

        public static List<PricingCostOption> CreateDefaultOptions() => new()
        {
            new PricingCostOption { Label = "Consumables / adhesive", IsIncluded = false, Value = 0m, ValueType = FixedValueType },
            new PricingCostOption { Label = "Risk / warranty reserve", IsIncluded = false, Value = 0m, ValueType = PercentValueType }
        };

        public static PricingSettings NormalizeSettings(PricingSettings? settings)
        {
            var normalized = settings ?? CreateDefaultSettings();
            normalized.ComplexityMultiplier = normalized.ComplexityMultiplier <= 0m ? 1m : normalized.ComplexityMultiplier;
            normalized.AverageJobsPerMonth = normalized.AverageJobsPerMonth <= 0m ? 1m : normalized.AverageJobsPerMonth;
            normalized.DiagnosisHours = Math.Max(0m, normalized.DiagnosisHours);
            normalized.RepairHours = Math.Max(0m, normalized.RepairHours);
            normalized.TechnicianHourlyRateSp = Math.Max(0m, normalized.TechnicianHourlyRateSp);
            normalized.DiagnosticInspectionFeeSp = Math.Max(0m, normalized.DiagnosticInspectionFeeSp);
            normalized.DefaultPartsMarkupPercent = Math.Max(0m, normalized.DefaultPartsMarkupPercent);
            normalized.MonthlyFixedOverheadSp = Math.Max(0m, normalized.MonthlyFixedOverheadSp);
            normalized.DesiredProfitMarginPercent = Math.Max(0m, normalized.DesiredProfitMarginPercent);
            normalized.VatTaxRatePercent = Math.Max(0m, normalized.VatTaxRatePercent);
            normalized.AdditionalCosts = NormalizeOptions(normalized.AdditionalCosts);
            return normalized;
        }

        public static List<PricingCostOption> NormalizeOptions(IEnumerable<PricingCostOption>? options)
        {
            return options?.Where(o => !string.IsNullOrWhiteSpace(o.Label)).Select(o => new PricingCostOption
            {
                Label = o.Label.Trim(),
                IsIncluded = o.IsIncluded,
                Value = o.Value,
                ValueType = NormalizeValueType(o.ValueType)
            }).ToList() ?? new List<PricingCostOption>();
        }

        public static PricingCalculationResult Calculate(IEnumerable<UsedPartLine> usedParts, PricingSettings settings, decimal usdRate)
        {
            var result = new PricingCalculationResult();
            var normalized = NormalizeSettings(settings);
            var safeRate = usdRate > 0 ? usdRate : 1m;

            if (normalized.IncludeDiagnosticFee)
            {
                result.DiagnosticFeeSp = normalized.WaiveDiagnosticIfRepairProceeds ? 0m : normalized.DiagnosticInspectionFeeSp;
            }

            if (normalized.IncludeLabourCost)
            {
                var totalHours = normalized.DiagnosisHours + normalized.RepairHours;
                result.LabourCostSp = normalized.TechnicianHourlyRateSp * totalHours * normalized.ComplexityMultiplier;
            }

            result.PartsCostSp = usedParts?.Sum(line => ConvertToSp((decimal)line.UnitPrice, line.PriceCurrency, safeRate) * Math.Max(1, line.Quantity)) ?? 0m;
            if (normalized.IncludePartsAndComponents)
            {
                result.PartsMarkupSp = result.PartsCostSp * normalized.DefaultPartsMarkupPercent / 100m;
                result.PartsSubtotalSp = result.PartsCostSp + result.PartsMarkupSp;
            }

            if (normalized.IncludeOverheadAllocation)
            {
                result.OverheadPerJobSp = normalized.MonthlyFixedOverheadSp / normalized.AverageJobsPerMonth;
            }

            var directCost = result.DiagnosticFeeSp + result.LabourCostSp + result.PartsSubtotalSp + result.OverheadPerJobSp;

            foreach (var option in normalized.AdditionalCosts.Where(o => o.IsIncluded))
            {
                var amount = NormalizeValueType(option.ValueType) == PercentValueType
                    ? directCost * option.Value / 100m
                    : option.Value;

                result.AdditionalCostsSp += amount;
                directCost += amount;
                AddLine(result, option.Label, amount);
            }

            result.TotalDirectCostSp = directCost;

            if (normalized.IncludeProfitMargin)
            {
                result.ProfitAmountSp = result.TotalDirectCostSp * normalized.DesiredProfitMarginPercent / 100m;
            }

            result.PreTaxRepairPriceSp = result.TotalDirectCostSp + result.ProfitAmountSp;

            if (normalized.IncludeVatTax)
            {
                result.VatAmountSp = result.PreTaxRepairPriceSp * normalized.VatTaxRatePercent / 100m;
            }

            result.FinalCostSp = Math.Round(result.PreTaxRepairPriceSp + result.VatAmountSp, 0, MidpointRounding.AwayFromZero);

            AddLine(result, "Diagnostic Fee", result.DiagnosticFeeSp);
            AddLine(result, "Labour Cost", result.LabourCostSp);
            AddLine(result, "Parts & Materials", result.PartsSubtotalSp);
            AddLine(result, "Overhead", result.OverheadPerJobSp);
            AddLine(result, "Profit", result.ProfitAmountSp);
            AddLine(result, "VAT / Tax", result.VatAmountSp);
            AddLine(result, "TOTAL (incl. VAT)", result.FinalCostSp);

            result.Breakdown.AddRange(new[]
            {
                CreateBreakdown("Diagnostic Fee", result.DiagnosticFeeSp, result.PreTaxRepairPriceSp),
                CreateBreakdown("Labour Cost", result.LabourCostSp, result.PreTaxRepairPriceSp),
                CreateBreakdown("Parts & Materials", result.PartsSubtotalSp, result.PreTaxRepairPriceSp),
                CreateBreakdown("Overhead", result.OverheadPerJobSp, result.PreTaxRepairPriceSp),
                CreateBreakdown("Profit", result.ProfitAmountSp, result.PreTaxRepairPriceSp),
                CreateBreakdown("TOTAL (pre-tax)", result.PreTaxRepairPriceSp, result.PreTaxRepairPriceSp)
            });

            return result;
        }

        public static decimal ConvertToSp(decimal amount, string? currency, decimal usdRate)
        {
            if (string.Equals(currency?.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
            {
                return amount * (usdRate > 0 ? usdRate : 1m);
            }

            return amount;
        }

        public static string NormalizeValueType(string? valueType)
            => string.Equals(valueType?.Trim(), PercentValueType, StringComparison.OrdinalIgnoreCase) ? PercentValueType : FixedValueType;

        private static void AddLine(PricingCalculationResult result, string label, decimal amount)
            => result.Lines.Add($"{label}: {amount.ToString("N0", CultureInfo.CurrentCulture)} S.P");

        private static PricingBreakdownLine CreateBreakdown(string component, decimal amount, decimal preTax)
            => new()
            {
                Component = component,
                AmountSp = amount,
                PercentOfPreTax = preTax == 0m ? 0m : amount / preTax * 100m
            };
    }
}
