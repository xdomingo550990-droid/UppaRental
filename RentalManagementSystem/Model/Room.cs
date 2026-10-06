namespace RentalManagementSystem.Model
{
    public class Room
    {
        // Auto-properties with default empty strings to avoid CS8618 nullability warnings
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;   // "Property / Unit Name or Number"
        public string Location { get; set; } = string.Empty;
        public int? Floor { get; set; }                          // optional, so it can be empty (NULL)
        public int? RoomTypeId { get; set; }                     // link to the RoomType table
        public decimal RentalRate { get; set; }                  // monthly rent
        public decimal SecurityDeposit { get; set; }
        public int Capacity { get; set; }

        // Details from the Add / Edit Property screens
        public int Bedrooms { get; set; }                        // 0 = studio
        public int Bathrooms { get; set; }
        public decimal? Size { get; set; }
        public string SizeUnit { get; set; } = "sqft";           // "sqft" or "sqm"
        public string LeaseTerm { get; set; } = "Long term";     // "Short term" or "Long term"

        // Utility billing setup (the 3 checkboxes)
        public bool WaterIncluded { get; set; }
        public bool ElectricitySeparate { get; set; }
        public bool WifiIncluded { get; set; }

        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;        // internal landlord notes

        public string Status { get; set; } = string.Empty;       // Draft, Available, Reserved, Occupied
        public bool IsFavorite { get; set; }                     // heart icon on Overview
        public int InquiryCount { get; set; }                    // "Most Popular" ranking
        public DateTime CreatedAt { get; set; }

        // Display only: filled by the DAO with a JOIN, NOT a database column
        public string RoomTypeName { get; set; } = string.Empty;

        // Filled from the room_amenities and room_photos tables
        public List<string> Amenities { get; set; } = new List<string>();
        public List<string> PhotoPaths { get; set; } = new List<string>();

        // Parameterless Constructor (for WPF data binding)
        public Room() { }

        // Parameterized Constructor (your original seven fields)
        public Room(int roomId, string roomNumber, int floor, string roomType,
            decimal rentalRate, int capacity, string status)
        {
            RoomId = roomId;
            RoomNumber = roomNumber;
            Floor = floor;
            RoomTypeName = roomType;
            RentalRate = rentalRate;
            Capacity = capacity;
            Status = status;
        }
    }
}