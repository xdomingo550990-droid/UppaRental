namespace RentalManagementSystem.Model
{
    public class RentalTerm
    {
        // Auto-properties replacing Java fields and getters/setters
        public int TermId { get; set; }
        public int Duration { get; set; }
        public decimal DownPaymentRate { get; set; } // Using decimal for financial percentages/rates 
                                                     // percent: 20 means 20%

        // Parameterless Constructor (for WPF data binding)
        public RentalTerm() { }

        // Parameterized Constructor
        public RentalTerm(int termId, int duration, decimal downPaymentRate)
        {
            TermId = termId;
            Duration = duration;
            DownPaymentRate = downPaymentRate;
        }
    }
}