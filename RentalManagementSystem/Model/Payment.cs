using System;

namespace RentalManagementSystem.Model
{
    public class Payment
    {
        // Auto-properties replacing Java fields and getters/setters
        public int PaymentId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;

        // Parameterless Constructor (for WPF data binding)
        public Payment() { }

        // Parameterized Constructor
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