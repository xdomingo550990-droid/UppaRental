using System;

namespace RentalManagementSystem.Model
{
    public class Receipt
    {
        // Auto-properties replacing Java fields and getters/setters
        public int ReceiptId { get; set; }
        public int PaymentId { get; set; }                          // the payment this receipt was issued for
        public string ReceiptNumber { get; set; } = string.Empty;   // e.g. RCT-0001
        public DateTime ReceiptDate { get; set; } = DateTime.Now;

        // Parameterless Constructor (for WPF data binding)
        public Receipt() { }

        // Parameterized Constructor (your original three fields)
        public Receipt(int receiptId, string receiptNumber, DateTime receiptDate)
        {
            ReceiptId = receiptId;
            ReceiptNumber = receiptNumber;
            ReceiptDate = receiptDate;
        }

        // Constructor with the payment link
        public Receipt(int receiptId, int paymentId, string receiptNumber, DateTime receiptDate)
            : this(receiptId, receiptNumber, receiptDate)
        {
            PaymentId = paymentId;
        }
    }
}