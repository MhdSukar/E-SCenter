using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ESCenter.Models
{
    public class RepairTicket
    {
        // ================= PRIMARY =================
        public int TicketId { get; set; }

        // Unique Identifier (Auto-generated)
        public string EscTicketId { get; set; }

        // ================= CUSTOMER =================
        public long? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string ContactMethod { get; set; }

        // ================= DEVICE =================
        public string DeviceCategory { get; set; }
        public string DeviceBrand { get; set; }
        public string DeviceModel { get; set; }
        public string SerialIMEI { get; set; }
        public string DamageHistory { get; set; }
        public string BoardModifications { get; set; }

        // ================= PROBLEM =================
        public string ProblemDescription { get; set; }
        public string Notes { get; set; }

        // ================= STATUS =================
        public string RepairStatus { get; set; }
        public string PriorityLevel { get; set; }

        // ================= COST =================
        public decimal? EstimatedCost { get; set; }
        public string EstimatedCostCurrency { get; set; } = "EUR";

        public decimal? FinalCost { get; set; }
        public string FinalCostCurrency { get; set; } = "EUR";

        // ================= WARRANTY =================
        public bool HasWarranty { get; set; }
        public string WarrantyPeriod { get; set; }
        public bool IsWarrantyRepair { get; set; }

        // ================= ROOT / PARTS =================
        public string RootCause { get; set; }
        public string PartsUsed { get; set; }

        // ================= DATES / FLAGS =================
        public DateTime ReceiveDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public bool IsReadyForPickup { get; set; }

        // ================= JSON STORAGE =================
        public string DeviceChecklistJson { get; set; }
        public string AccessoriesJson { get; set; }

        // ================= UI CONVENIENCE =================
        [JsonIgnore]
        public DeviceChecklist DeviceChecklist
        {
            get => DeserializeOrDefault(DeviceChecklistJson, new DeviceChecklist());
            set => DeviceChecklistJson = JsonSerializer.Serialize(value ?? new DeviceChecklist());
        }

        [JsonIgnore]
        public Accessories Accessories
        {
            get => DeserializeOrDefault(AccessoriesJson, new Accessories());
            set => AccessoriesJson = JsonSerializer.Serialize(value ?? new Accessories());
        }

        public string FinalCostDisplay
        {
            get
            {
                if (FinalCost.HasValue)
                {
                    return $"{FinalCost.Value:F0} {FinalCostCurrency ?? "USD"}";
                }

                return "-";
            }
        }

        private static T DeserializeOrDefault<T>(string json, T fallback)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return fallback;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json) ?? fallback;
            }
            catch (JsonException)
            {
                return fallback;
            }
        }
    }

    // ================= DEVICE CHECKLIST =================
    public class DeviceChecklist
    {
        public bool DeviceCheckPowerOn { get; set; }
        public bool DeviceCheckBoots { get; set; }
        public bool DeviceCheckBatteryCharging { get; set; }
        public bool DeviceCheckBatteryHealth { get; set; }
        public bool DeviceCheckOverheating { get; set; }
        public bool DeviceCheckScreen { get; set; }
        public bool DeviceCheckTouch { get; set; }
        public bool DeviceCheckDeadPixels { get; set; }
        public bool DeviceCheckButtons { get; set; }
        public bool DeviceCheckSpeaker { get; set; }
        public bool DeviceCheckMicrophone { get; set; }
        public bool DeviceCheckFrontCamera { get; set; }
        public bool DeviceCheckRearCamera { get; set; }
        public bool DeviceCheckWiFi { get; set; }
        public bool DeviceCheckBluetooth { get; set; }
        public bool DeviceCheckCellular { get; set; }
        public bool DeviceCheckPorts { get; set; }
        public bool DeviceCheckBiometrics { get; set; }
        public bool DeviceCheckActivationLock { get; set; }
        public bool DeviceCheckStorage { get; set; }
        public bool DeviceCheckFrame { get; set; }
        public bool DeviceCheckCover { get; set; }
        public bool DeviceCheckLiquidDamage { get; set; }
        public bool DeviceCheckPreviousRepair { get; set; }
    }

    // ================= ACCESSORIES =================
    public class Accessories
    {
        public bool AccessorySoftware { get; set; }
        public bool AccessoryUSBStick { get; set; }
        public bool AccessoryCase { get; set; }
        public bool AccessoryCharger { get; set; }
        public bool AccessoryBattery { get; set; }
        public bool AccessoryMonitor { get; set; }
        public bool AccessoryPowerCord { get; set; }
        public bool AccessoryOriginalPackage { get; set; }
        public bool AccessoryMemoryCard { get; set; }
        public bool AccessoryCables { get; set; }
        public bool AccessoryMouse { get; set; }
        public bool AccessoryKeyboard { get; set; }
        public bool AccessorySIMCard { get; set; }
        public bool AccessorySDAdapter { get; set; }
        public bool AccessoryStylus { get; set; }
        public bool AccessoryDock { get; set; }
        public bool AccessoryExternalDrive { get; set; }
        public bool AccessoryHeadphones { get; set; }
    }
}
