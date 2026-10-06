using System;

namespace RentalManagementSystem.Model
{
    public class Payment
    {
        // Auto-properties replacing Java fields and getters/setters
        public int PaymentId { get; set; }

        // What this payment is for (fill ONE of the first two)
        public int? BillId { get; set; }           // paying a bill
        public int? ReservationId { get; set; }    // paying a reservation down payment (no bill yet)
        public int? UserId { get; set; }           // staff who received the payment

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";           // Cash, GCash, Bank Transfer, Credit/Debit Card
        public string ReferenceNumber { get; set; } = string.Empty;   // GCash / bank reference (can be empty)
        public DateTime CreatedAt { get; set; }

        // Display only: filled by the DAO with a JOIN, NOT a database column
        public string RenterName { get; set; } = string.Empty;

        // Parameterless Constructor (for WPF data binding)
        public Payment() { }

        // Parameterized Constructor (your original five fields)
        public Payment(int paymentId,
            DateTime paymentDate,
            decimal amount,
            string paymentMethod,
            string referenceNumber)
        {
            PaymentId = paymentId;
            PaymentDate = paymentDate;
            Amount = amount;
            PaymentMethod = paymentMethod;
            ReferenceNumber = referenceNumber;
        }
    }
}