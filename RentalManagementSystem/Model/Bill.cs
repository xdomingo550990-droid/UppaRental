using System;

namespace RentalManagementSystem.Model
{
    public class Bill
    {
        // Auto-properties replacing Java fields and getters/setters
        public int BillId { get; set; }
        public DateTime BillDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = string.Empty;

        // Parameterless Constructor (for WPF data binding)
        public Bill() { }

        // Parameterized Constructor
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