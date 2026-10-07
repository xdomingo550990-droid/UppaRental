using System;

namespace RentalManagementSystem.Model
{
    public class Report
    {
        // Auto-properties replacing Java fields and getters/setters
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;    // Rental Report, Payment Report, Occupancy / Unit Status
        public DateTime DateGenerated { get; set; } = DateTime.Now;

        // Filters used when the report was generated (Start date / End date on the Reports page)
        public DateTime? PeriodStart { get; set; }                // can be empty (NULL)
        public DateTime? PeriodEnd { get; set; }                  // can be empty (NULL)

        // Who generated it
        public int? GeneratedBy { get; set; }                     // UserId, can be empty (NULL)

        // Parameterless Constructor (for WPF data binding)
        public Report() { }

        // Parameterized Constructor (your original three fields)
        public Report(int reportId, string reportType, DateTime dateGenerated)
        {
            ReportId = reportId;
            ReportType = reportType;
            DateGenerated = dateGenerated;
        }
    }
}