using System;

namespace RentalManagementSystem.Model
{
    public class Bill
    {
        // Auto-properties replacing Java fields and getters/setters
        public int BillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;       // e.g. INV-0001
        public int ReservationId { get; set; }                       // which reservation this bill belongs to

        public string Category { get; set; } = "Monthly Rent";       // Monthly Rent, Water Bill, Electric Bill, Security Deposit, Penalty / Late Fee
        public DateTime BillDate { get; set; } = DateTime.Now;
        public string BillingPeriod { get; set; } = string.Empty;    // e.g. "Oct 2026"
        public DateTime DueDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }
        public decimal Balance { get; set; }                         // amount still unpaid
        public string Status { get; set; } = "Pending";              // Pending, Paid, Overdue
        public DateTime CreatedAt { get; set; }

        // Display only: filled by the DAO with a JOIN, NOT database columns
        public string RenterName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;

        // Parameterless Constructor (for WPF data binding)
        public Bill() { }

        // Parameterized Constructor (your original five fields)
        public Bill(int billId,
            DateTime billDate,
            decimal totalAmount,
            decimal balance,
            string status)
        {
            BillId = billId;
            BillDate = billDate;
            TotalAmount = totalAmount;
            Balance = balance;
            Status = status;
        }
    }
}