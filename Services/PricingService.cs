using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ESCenter.Models;

namespace ESCenter.Services
{
    public sealed class PricingCalculationResult
    {
        public decimal PartsCostSp { get; set; }
        public decimal AdjustmentsSp { get; set; }
        public decimal FinalCostSp { get; set; }
        public List<string> Lines { get; } = new();
    }

    public static class PricingService
    {
        public const string FixedValueType = "Fixed";
        public const string PercentValueType = "Percent";

        public static List<PricingCostOption> CreateDefaultOptions() => new()
        {
            new PricingCostOption { Label = "Include parts used cost", IsIncluded = true, Value = 0m, ValueType = FixedValueType },
            new PricingCostOption { Label = "Repair labor", IsIncluded = true, Value = 0m, ValueType = FixedValueType },
            new PricingCostOption { Label = "Diagnosis cost", IsIncluded = false, Value = 0m, ValueType = FixedValueType },
            new PricingCostOption { Label = "Warranty / risk margin", IsIncluded = false, Value = 0m, ValueType = PercentValueType },
            new PricingCostOption { Label = "Profit margin", IsIncluded = false, Value = 0m, ValueType = PercentValueType },
            new PricingCostOption { Label = "Discount", IsIncluded = false, Value = 0m, ValueType = PercentValueType }
        };

        public static List<PricingCostOption> NormalizeOptions(IEnumerable<PricingCostOption>? options)
        {
            var rows = options?.Where(o => !string.IsNullOrWhiteSpace(o.Label)).Select(o => new PricingCostOption
            {
                Label = o.Label.Trim(),
                IsIncluded = o.IsIncluded,
                Value = o.Value,
                ValueType = NormalizeValueType(o.ValueType)
            }).ToList() ?? new List<PricingCostOption>();

            return rows.Count > 0 ? rows : CreateDefaultOptions();
        }

        public static PricingCalculationResult Calculate(IEnumerable<UsedPartLine> usedParts, IEnumerable<PricingCostOption> options, decimal usdRate)
        {
            var result = new PricingCalculationResult();
            var safeRate = usdRate > 0 ? usdRate : 1m;
            var rows = NormalizeOptions(options);
            var includeParts = rows.FirstOrDefault(IsPartsCostRow)?.IsIncluded ?? true;

            result.PartsCostSp = usedParts?.Sum(line => ConvertToSp((decimal)line.UnitPrice, line.PriceCurrency, safeRate) * Math.Max(1, line.Quantity)) ?? 0m;

            decimal subtotal = includeParts ? result.PartsCostSp : 0m;
            if (includeParts)
            {
                result.Lines.Add($"Parts used: {result.PartsCostSp.ToString("N0", CultureInfo.CurrentCulture)} S.P");
            }

            foreach (var option in rows.Where(o => !IsPartsCostRow(o) && o.IsIncluded))
            {
                var delta = NormalizeValueType(option.ValueType) == PercentValueType
                    ? subtotal * option.Value / 100m
                    : option.Value;

                subtotal += delta;
                result.AdjustmentsSp += delta;
                result.Lines.Add($"{option.Label}: {delta.ToString("N0", CultureInfo.CurrentCulture)} S.P");
            }

            result.FinalCostSp = Math.Round(subtotal, 0, MidpointRounding.AwayFromZero);
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

        private static bool IsPartsCostRow(PricingCostOption option)
            => option.Label.Contains("parts", StringComparison.OrdinalIgnoreCase);
    }
}
