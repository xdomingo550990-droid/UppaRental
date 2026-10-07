using System;

namespace RentalManagementSystem.Model
{
    public class User
    {
        // Properties
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Role Role { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Computed Property for Full Name
        public string FullName
        {
            get => $"{FirstName} {LastName}".Trim();
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string[] parts = value.Trim().Split(' ', 2);
                    FirstName = parts[0];
                    LastName = parts.Length > 1 ? parts[1] : string.Empty;
                }
            }
        }

        // --- Constructors ---

        public User() { }

        public User(int userId)
        {
            UserId = userId;
        }

        public User(int userId, string firstName, string lastName, Role role, string emailAddress, string password, string gender)
        {
            UserId = userId;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
            EmailAddress = emailAddress;
            Password = password;
            Gender = gender;
        }

        public User(int userId, string username, string password, Role role)
        {
            UserId = userId;
            Username = username;
            Password = password;
            Role = role;
        }

        public User(int userId, string username, string password, string role)
            : this(userId, username, password, ParseRole(role))
        {
        }

        public User(int userId, string username, string password, Role role,
            string fullName, string email, string phone, DateTime createdAt)
            : this(userId, username, password, role)
        {
            FullName = fullName;
            EmailAddress = email;
            Phone = phone;
            CreatedAt = createdAt;
        }

        public User(int userId, string username, string password, string role,
            string fullName, string email, string phone, DateTime createdAt)
            : this(userId, username, password, ParseRole(role), fullName, email, phone, createdAt)
        {
        }

        // --- Helper Methods ---

        public static Role ParseRole(string roleStr)
        {
            if (Enum.TryParse<Role>(roleStr, true, out var result))
            {
                return result;
            }
            return Role.Tenant;
        }

        // --- Legacy Getter / Setter Wrappers (For Backwards Compatibility) ---
        public int getUserId() => UserId;
        public void setUserId(int id) => UserId = id;
        public string getFirstName() => FirstName;
        public void setFirstName(string name) => FirstName = name;
        public string getLastName() => LastName;
        public void setLastName(string name) => LastName = name;
        public string getUsername() => Username;
        public void setUsername(string name) => Username = name;
        public string getPassword() => Password;
        public void setPassword(string pwd) => Password = pwd;
        public Role getRole() => Role;
        public void setRole(Role r) => Role = r;
        public void setRole(string r) => Role = ParseRole(r);
        public string getEmailAddress() => EmailAddress;
        public void setEmailAddress(string email) => EmailAddress = email;
        public string getGender() => Gender;
        public void setGender(string g) => Gender = g;
        public string getAge() => Age;
        public void setAge(string a) => Age = a;
        public string getPhone() => Phone;
        public void setPhone(string p) => Phone = p;
        public DateTime getCreatedAt() => CreatedAt;
        public void setCreatedAt(DateTime dt) => CreatedAt = dt;
        public string getFullName() => FullName;
        public void setFullName(string fn) => FullName = fn;

    }
}