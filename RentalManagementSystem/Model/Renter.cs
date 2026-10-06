using System;

namespace RentalManagementSystem.Model
{
    public class Renter
    {
        // Auto-properties with string.Empty initializers to prevent CS8618 warnings
        public int RenterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;       // Renters and Reservation forms
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Parameterless Constructor (for WPF data binding)
        public Renter() { }

        // Parameterized Constructor (your original four fields)
        public Renter(int renterId, string name, string contact, string address)
        {
            RenterId = renterId;
            Name = name;
            Contact = contact;
            Address = address;
        }
    }
}