namespace RentalManagementSystem.Model
{
    public class User
    {
        // Defaulting string properties to string.Empty clears the yellow warning
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;   // holds the HASH, never the real password
        public string Role { get; set; } = string.Empty;

        // Profile page
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Parameterless Constructor
        public User() { }

        // Constructor for login (the original four fields)
        public User(int userId, string username, string password, string role)
        {
            UserId = userId;
            Username = username;
            Password = password;
            Role = role;
        }

        // Constructor with every field
        public User(int userId, string username, string password, string role,
            string fullName, string email, string phone, DateTime createdAt)
            : this(userId, username, password, role)
        {
            FullName = fullName;
            Email = email;
            Phone = phone;
            CreatedAt = createdAt;
        }
    }
}