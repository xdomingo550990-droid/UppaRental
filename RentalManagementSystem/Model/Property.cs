using System;
using System.Collections.Generic;
using System.Text;

namespace RentalManagementSystem.Model
{
    public class Property {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int NumberOfRooms { get; set; }
        public int NumberOfFloors { get; set; }
        public int NumberofBathrooms { get; set; }
        public int MaximumCapacity { get; set; }
        public int SizeUnit { get; set; }
        public int SecurityDeposit { get; set; }
        public int MonthlyRent { get; set; }
        public Boolean isElectricIncluded { get; set; }
        public int ElectricBill { get; set; }
        public Boolean isWaterIncluded { get; set; }
        public int WaterBill { get; set; }

        public Boolean isWifiIncluded { get; set; }
        public int WifiBill { get; set; }
        public Amenities Amenities { get; set; } = new Amenities();

        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;        // internal landlord notes

        public string Status { get; set; } = string.Empty;       // Draft, Available, Reserved, Occupied
        public bool IsFavorite { get; set; }                     // heart icon on Overview
        public int InquiryCount { get; set; }                    // "Most Popular" ranking
        public DateTime CreatedAt { get; set; }
        public PropertyType PropertyType { get; set; } = new PropertyType();
        public List<string> PhotoPaths { get; set; } = new List<string>();
        public Reservation Reserve { get; set; } = new Reservation();

    }
}
