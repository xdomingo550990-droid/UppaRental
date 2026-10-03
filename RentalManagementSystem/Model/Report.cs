using System;

namespace RentalManagementSystem.Model
{
    public class Report
    {
        // Auto-properties replacing Java fields and getters/setters
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public DateTime DateGenerated { get; set; } = DateTime.Now;

        // Parameterless Constructor (for WPF data binding)
        public Report() { }

        // Parameterized Constructor
        public Report(int reportId, string reportType, DateTime dateGenerated)
        {
            ReportId = reportId;
            ReportType = reportType;
            DateGenerated = dateGenerated;
        }
    }
}