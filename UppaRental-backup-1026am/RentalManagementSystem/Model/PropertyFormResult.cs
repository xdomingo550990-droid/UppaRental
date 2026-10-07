using System;
using System.Collections.Generic;
using System.Text;

namespace RentalManagementSystem.Model
{
    public class PropertyFormResult
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int NumberOfRooms { get; set; }
        public int NumberOfFloors { get; set; }
        public int NumberOfBathrooms { get; set; }
        public int MaximumCapacity { get; set; }
        public int SizeUnit { get; set; }
        public int MonthlyRent { get; set; }
        public int SecurityDeposit { get; set; }
        public PropertyType PropertyType { get; set; } = PropertyType.Studio;
        public List<Amenities> Amenities { get; set; } = new List<Amenities>();
        public List<string> PhotoPaths { get; set; } = new List<string>();
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = "Available";
    }
}
