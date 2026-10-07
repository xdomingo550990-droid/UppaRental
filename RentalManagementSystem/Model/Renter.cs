using System;

namespace RentalManagementSystem.Model
{
    public class Renter : User
    {
        // Aliases linked directly to base User properties
        public int RenterId
        {
            get => UserId;
            set => UserId = value;
        }

        public string Contact
        {
            get => Phone;
            set => Phone = value;
        }

        public string Address { get; set; } = string.Empty;
        public RentalTerm TermRent { get; set; } = new RentalTerm();

        // Parameterless Constructor
        public Renter()
        {
            Role = Role.Tenant;
        }

        // Parameterized Constructor
        public Renter(int renterId, string name, string contact, string address)
        {
            UserId = renterId;
            FullName = name;
            Phone = contact;
            Address = address;
            Role = Role.Tenant;
        }
    }
}