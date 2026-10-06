namespace RentalManagementSystem.Model
{
    public class Room 
    {
        // Auto-properties with default empty strings to avoid CS8618 nullability warnings
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public decimal RentalRate { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;

        // Parameterless Constructor (for WPF data binding)
        public Room() { }

        // Parameterized Constructor
        public Room(int roomId, string roomNumber, int floor, string roomType, 
            decimal rentalRate, int capacity, string status)
        {
            RoomId = roomId;
            RoomNumber = roomNumber;
            Floor = floor;
            RoomType = roomType;
            RentalRate = rentalRate;
            Capacity = capacity;
            Status = status;
        }
    }
}