using RentalManagementSystem.Model;
using System.Collections.Generic;

namespace RentalManagementSystem.Presentation
{
    public class PropertyFormResult
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int NumberOfFloors { get; set; } = 1;
        public int NumberOfRooms { get; set; } = 1;
        public int NumberOfBathrooms { get; set; } = 1;
        public int MaximumCapacity { get; set; } = 2;
        public int SizeUnit { get; set; }
        public PropertyType PropertyType { get; set; } = PropertyType.Studio;
        public int MonthlyRent { get; set; }
        public int SecurityDeposit { get; set; }
        public List<Amenities> Amenities { get; set; } = new List<Amenities>();
        public List<string> PhotoPaths { get; set; } = new List<string>();
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = "Available";
    }
}
