using RentalManagementSystem.Presentation;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Change this namespace to match where your RentalProperty class lives
namespace RentalManagementSystem
{
    public static class RentalPropertyExtras
    {
        private sealed class Extras
        {
            public int MaxCapacity = 1;
            public UtilitySettings Utilities = new UtilitySettings();
            public List<string> Amenities = new List<string>();
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

        public static List<string> GetAmenities(this RentalProperty p) =>
            Table.GetOrCreateValue(p).Amenities;

        public static void SetAmenities(this RentalProperty p, List<string> value) =>
            Table.GetOrCreateValue(p).Amenities = value ?? new List<string>();
    }
}