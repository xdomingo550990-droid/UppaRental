using System;
using System.Collections.Generic;

namespace RentalManagementSystem.Model
{
    public class Property
    {
        public int PropertyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int NumberOfRooms { get; set; }
        public int NumberOfFloors { get; set; }
        public int NumberofBathrooms { get; set; }
        public int MaximumCapacity { get; set; }
        public int SizeUnit { get; set; }
        public int SecurityDeposit { get; set; }
        public int MonthlyRent { get; set; }

        // --- Electricity & kWh Utilities ---
        public bool isElectricIncluded { get; set; }
        public int ElectricBill { get; set; }
        public double ElectricKwhRate { get; set; } = 12.00; // e.g., ₱12 per kWh
        public double ElectricPreviousReading { get; set; }
        public double ElectricCurrentReading { get; set; }

        // --- Water & Meter Utilities ---
        public bool isWaterIncluded { get; set; }
        public int WaterBill { get; set; }
        public double WaterRatePerCubicMeter { get; set; } = 50.00; // e.g., ₱50 per m³
        public double WaterPreviousReading { get; set; }
        public double WaterCurrentReading { get; set; }

        // --- Wi-Fi Utility ---
        public bool isWifiIncluded { get; set; }
        public int WifiBill { get; set; }

        // Change this:
        public int? LandlordId { get; set; }        
        // public Amenities Amenities { get; set; } = new Amenities();

        // To this:
        public List<Amenities> Amenities { get; set; } = new List<Amenities>();
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Draft, Available, Reserved, Occupied
        public bool IsFavorite { get; set; }
        public int InquiryCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // Change this:
        // public PropertyType PropertyType { get; set; } = new PropertyType();
        // =========================================================================
        // UI Display Properties (Required by PropertyCardStyle.xaml bindings)
        // =========================================================================

        public string Photo => PhotoPaths != null && PhotoPaths.Count > 0
            ? PhotoPaths[0]
            : "pack://application:,,,/Resources/Images/houseImage1.jpg"; // first uploaded photo, else the default house image

        public string BedroomsText => $"{NumberOfRooms} {(NumberOfRooms == 1 ? "Bed" : "Beds")}";

        public string BathroomsText => $"{NumberofBathrooms} {(NumberofBathrooms == 1 ? "Bath" : "Baths")}";

        public string SqFtText => $"{SizeUnit} sqft";

        public string Term => "1 Year"; // Default lease term display

        public string PriceText => $"₱{MonthlyRent:N0}";

        public string PriceUnit => "/mo";

        public string PriceWithUnit => $"₱{MonthlyRent:N0}/mo";

        public string Summary => $"{BedroomsText} · {BathroomsText} · {SqFtText}";
        // To this:
        public PropertyType PropertyType { get; set; } = PropertyType.Studio; public List<string> PhotoPaths { get; set; } = new List<string>();
        public Reservation Reserve { get; set; } = new Reservation();

        // Utility Bill Computation Methods
        public decimal CalculateElectricBill()
        {
            if (isElectricIncluded) return 0m;
            double usageKwh = Math.Max(0, ElectricCurrentReading - ElectricPreviousReading);
            return (decimal)(usageKwh * ElectricKwhRate);
        }

        public decimal CalculateWaterBill()
        {
            if (isWaterIncluded) return 0m;
            double usageCubicMeters = Math.Max(0, WaterCurrentReading - WaterPreviousReading);
            return (decimal)(usageCubicMeters * WaterRatePerCubicMeter);
        }
    }
}