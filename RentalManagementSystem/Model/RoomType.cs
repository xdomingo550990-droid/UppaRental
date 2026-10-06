namespace RentalManagementSystem.Model
{
    public class RoomType
    {
        // Auto-implemented properties replacing private fields and getters/setters
        public int RoomTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public decimal RentalRate { get; set; } // Using decimal for monetary values in C#
        public int Capacity { get; set; }

        // Parameterless Constructor (for WPF data binding)
        public RoomType() { }

        // Parameterized Constructor
        public RoomType(int roomTypeId, string typeName, decimal rentalRate, int capacity)
        {
            RoomTypeId = roomTypeId;
            TypeName = typeName;
            RentalRate = rentalRate;
            Capacity = capacity;
        }
        
        // Lets a ComboBox show the type name (for example "Studio")
        public override string ToString() => TypeName;
    }
}