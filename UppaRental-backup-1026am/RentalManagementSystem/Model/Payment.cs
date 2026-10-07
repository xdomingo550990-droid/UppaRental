using System;

namespace RentalManagementSystem.Model
{
    public class Payment
    {
        public int PaymentId { get; set; }

        // Foreign Key ID (used for database queries)
        public int UserId { get; set; }

        // Optional Navigation Property (populated when joining models)
        public User? User { get; set; }

        // What this payment is for (fill ONE of these)
        public int? BillId { get; set; }           // Paying a bill
        public int? ReservationId { get; set; }    // Paying a reservation down payment

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";           // Cash, GCash, Bank Transfer, Credit/Debit Card
        public string ReferenceNumber { get; set; } = string.Empty;   // GCash / bank reference
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Display only: filled by JOINs in DAOs/Services
        public string RenterName { get; set; } = string.Empty;

        // Parameterless Constructor (for WPF Data Binding)
        public Payment() { }

        // Parameterized Constructor
        public Payment(int paymentId,
                       DateTime paymentDate,
                       decimal amount,
                       string paymentMethod,
                       string referenceNumber,
                       User user)
        {
            PaymentId = paymentId;
            PaymentDate = paymentDate;
            Amount = amount;
            PaymentMethod = paymentMethod;
            ReferenceNumber = referenceNumber;
            User = user;
            UserId = user?.getUserId() ?? user?.UserId ?? 0;
        }
    }
}