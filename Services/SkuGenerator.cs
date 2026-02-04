using System;

namespace ESCenter.Services
{
    public static class SkuGenerator
    {
        public static string Generate(string partType, string unit1Value, string unit1Code, string unit2Value, string unit2Code)
        {
            // Prefix based on part type
            var prefix = partType switch
            {
                "Resistors" => "RES",
                "Capacitors" => "CAP",
                "ICs" => "IC",
                "Coils" => "COIL",
                "Ports" => "PORT",
                "Transistors" => "TR",
                "Diodes" => "DIO",
                _ => "GEN"
            };

            // Build Unit1 string
            var unit1 = string.IsNullOrWhiteSpace(unit1Value) ? "NA" : $"{unit1Value}{unit1Code}";

            // Build Unit2 string
            var unit2 = string.IsNullOrWhiteSpace(unit2Value) ? "NA" : $"{unit2Value}{unit2Code}";

            return $"{prefix}-{unit1}-{unit2}";
        }
    }
}
