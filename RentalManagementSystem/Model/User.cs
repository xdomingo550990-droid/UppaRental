namespace RentalManagementSystem.Model
{
    public class User
    {
        // Defaulting string properties to string.Empty clears the yellow warning
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // Parameterless Constructor
        public User() { }

        // Parameterized Constructor
        public User(int userId, string username, string password, string role)
        {
            UserId = userId;
            Username = username;
            Password = password;
            Role = role;
        }
    }
}