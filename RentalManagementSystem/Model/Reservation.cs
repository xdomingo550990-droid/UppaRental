using System;

namespace RentalManagementSystem.Model
{
    public class Reservation
    {
        // Auto-properties replacing Java fields and getters/setters
        public int ReservationId { get; set; }
        public DateTime ReservationDate { get; set; } = DateTime.Now;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now;
        public int RentalDuration { get; set; }
        public decimal TotalRent { get; set; }
        public decimal DownPayment { get; set; }
        public string Status { get; set; } = string.Empty;

        // Parameterless Constructor (for WPF data binding)
        public Reservation() { }

        // Parameterized Constructor
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