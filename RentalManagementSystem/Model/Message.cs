using System;

namespace RentalManagementSystem.Model
{
    public class Message
    {
        // Auto-properties replacing Java fields and getters/setters
        public int MessageId { get; set; }
        public int RenterId { get; set; }                        // whose conversation this belongs to
        public string SentBy { get; set; } = "Landlord";         // "Landlord" or "Renter"
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; }

        // Not a database column: lets the chat bubble pick the right side and color
        public bool IsSentByLandlord => SentBy == "Landlord";

        // Display only: filled by the DAO with a JOIN, NOT a database column
        public string RenterName { get; set; } = string.Empty;

        // Parameterless Constructor (for WPF data binding)
        public Message() { }

        // Parameterized Constructor
        public Message(int messageId, int renterId, string sentBy, string content, DateTime sentAt, bool isRead)
        {
            MessageId = messageId;
            RenterId = renterId;
            SentBy = sentBy;
            Content = content;
            SentAt = sentAt;
            IsRead = isRead;
        }
    }
}