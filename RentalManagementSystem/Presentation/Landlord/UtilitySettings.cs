namespace RentalManagementSystem.Presentation
{
    /// <summary>How a utility is billed when it is NOT included in the rent.</summary>
    public enum UtilityBilling
    {
        TenantPaysProvider,
        FixedMonthlyFee,
        SplitAmongTenants,
        PerPersonRate,
        SubMetered
    }

    public class UtilityOption
    {
        public bool Included { get; set; }
        public UtilityBilling Billing { get; set; } = UtilityBilling.TenantPaysProvider;
        public decimal Amount { get; set; }
        public string Note { get; set; } = "";
    }

    public class UtilitySettings
    {
        public UtilityOption Water { get; set; } = new UtilityOption();
        public UtilityOption Electricity { get; set; } = new UtilityOption();
        public UtilityOption Wifi { get; set; } = new UtilityOption();
    }
}