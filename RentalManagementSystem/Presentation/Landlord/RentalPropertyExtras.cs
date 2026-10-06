using System.Runtime.CompilerServices;

namespace RentalManagementSystem.Presentation
{
    public static class RentalPropertyExtras
    {
        private sealed class Extras
        {
            public int MaxCapacity = 1;
            public UtilitySettings Utilities = new UtilitySettings();
        }

        private static readonly ConditionalWeakTable<RentalProperty, Extras> Table = new();

        public static int GetMaxCapacity(this RentalProperty p) =>
            Table.GetOrCreateValue(p).MaxCapacity;

        public static void SetMaxCapacity(this RentalProperty p, int value) =>
            Table.GetOrCreateValue(p).MaxCapacity = value;

        public static UtilitySettings GetUtilities(this RentalProperty p) =>
            Table.GetOrCreateValue(p).Utilities;

        public static void SetUtilities(this RentalProperty p, UtilitySettings value) =>
            Table.GetOrCreateValue(p).Utilities = value ?? new UtilitySettings();
    }
}