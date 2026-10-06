using System;

namespace RentalManagementSystem.Model
{
    public class Reservation
    {
        // Auto-properties replacing Java fields and getters/setters
        public User UserId { get; set; }    // User.UserId = index nato sa user na pangcheck if naay reservationsi index user
        public int ReservationId { get; set; }

        // Links to other tables (the foreign keys)
        public int RenterId { get; set; }          // who reserved
        public int RoomId { get; set; }            // which room
        public int? TermId { get; set; }           // rental term used (can be empty)
       
        public DateTime ReservationDate { get; set; } = DateTime.Now;
        public DateTime StartDate { get; set; } = DateTime.Now;     // move-in date
        public DateTime EndDate { get; set; } = DateTime.Now;
        public int RentalDuration { get; set; }                     // in months

        public decimal MonthlyRate { get; set; }                    // room rate at the time of booking
        public decimal TotalRent { get; set; }                      // MonthlyRate x RentalDuration
        public decimal DownPayment { get; set; }                    // TotalRent x downpayment rate

        public string Status { get; set; } = "Pending";             // Pending, Reserved, Active, Completed, Cancelled
        public DateTime CreatedAt { get; set; }

        // Display only: filled by the DAO with a JOIN, NOT database columns
        public string RenterName { get; set; } = string.Empty;
        public string RenterContact { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;

        // Parameterless Constructor (for WPF data binding)
        public Reservation() { }

        // Parameterized Constructor (your original eight fields)
        public Reservation(int reservationId,
            DateTime reservationDate,
            DateTime startDate,
            DateTime endDate,
            int rentalDuration,
            decimal totalRent,
            decimal downPayment,
            string status)
        {
            ReservationId = reservationId;
            ReservationDate = reservationDate;
            StartDate = startDate;
            EndDate = endDate;
            RentalDuration = rentalDuration;
            TotalRent = totalRent;
            DownPayment = downPayment;
            Status = status;
        }
    }
}