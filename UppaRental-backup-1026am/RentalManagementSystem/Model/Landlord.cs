namespace RentalManagementSystem.Model
{
    public class Landlord : User
    {
        public Property Properties { get; set; } = new Property();
        public int LandlordId { get; set; } 
        public Landlord()
        {
            Role = Role.Landlord;
        }

        public Landlord(int landlordid, int userId, string username, string password, string fullName, string email, string phone, DateTime createdAt)
            : base(userId, username, password, Role.Landlord, fullName, email, phone, createdAt)
        {
            LandlordId = landlordid;
        }
    }
}