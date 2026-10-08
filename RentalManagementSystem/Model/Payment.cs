using System;
using System.Globalization;

namespace RentalManagementSystem.Model
{
    public class Payment
    {
        public int PaymentId { get; set; }

        /// <summary>Foreign key to the paying user (used for database queries).</summary>
        public int UserId { get; set; }

        /// <summary>Optional navigation property, filled by DAO/JOIN or by the UI when available.</summary>
        public User? User { get; set; }

        // What this payment is for (fill ONE of these)
        public int? BillId { get; set; }           // Paying a bill
        public int? ReservationId { get; set; }    // Paying a reservation down payment

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";           // Cash, GCash, Bank Transfer, Credit/Debit Card, etc.
        public string ReferenceNumber { get; set; } = string.Empty;   // GCash / bank reference (e.g., BT-55120934)
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ---------- Display-only / extended properties (filled by DAO JOINs or the UI) ----------
        public string RenterName { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;     // e.g., "RCT-2951"
        public string InvoiceNumber { get; set; } = string.Empty;     // e.g., "INV-1993"
        public string Period { get; set; } = string.Empty;            // e.g., "Sep 2026"
        public string ReceiptUrl { get; set; } = string.Empty;        // Path or URL to the receipt PDF

        // Culture-invariant so the format doesn't change with the machine's regional settings
        public string FormattedDatePaid => PaymentDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);

        // Parameterless constructor (for WPF data binding / DAO mapping)
        public Payment() { }

        // Constructor taking a user id
        public Payment(
            int paymentId,
            DateTime paymentDate,
            decimal amount,
            string paymentMethod,
            string referenceNumber,
            int userId,
            string receiptNumber = "",
            string invoiceNumber = "",
            string period = "")
        {
            PaymentId = paymentId;
            PaymentDate = paymentDate;
            Amount = amount;
            PaymentMethod = paymentMethod;
            ReferenceNumber = referenceNumber;
            UserId = userId;
            ReceiptNumber = receiptNumber;
            InvoiceNumber = invoiceNumber;
            Period = period;
        }

        // Constructor taking a User object
        public Payment(
            int paymentId,
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
            UserId = user?.UserId ?? 0;
        }
    }
}