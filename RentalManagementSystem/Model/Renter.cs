namespace RentalManagementSystem.Model
{
    public class Renter
    {
        // Auto-properties with string.Empty initializers to prevent CS8618 warnings
        public int RenterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Parameterless Constructor (for WPF data binding)
        public Renter() { }

        // Parameterized Constructor
        public Renter(int renterId, string name, string contact, string address)
        {
            RenterId = renterId;
            Name = name;
            Contact = contact;
            Address = address;
        }
    }
}