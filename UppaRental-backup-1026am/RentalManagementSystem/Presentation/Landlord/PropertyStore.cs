#nullable disable
using RentalManagementSystem.Model;
using RentalManagementSystem.Presentation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RentalManagementSystem.Presentation
{
    /// <summary>
    /// Inherited attached property. A page sets Manage.ActionsVisibility="Visible" on its root
    /// to show the Edit / Delete links inside the property cards and rows.
    /// Pages that don't set it (like Overview) never show them.
    /// </summary>
    public static class Manage
    {
        public static readonly DependencyProperty ActionsVisibilityProperty =
            DependencyProperty.RegisterAttached(
                "ActionsVisibility", typeof(Visibility), typeof(Manage),
                new FrameworkPropertyMetadata(Visibility.Collapsed, FrameworkPropertyMetadataOptions.Inherits));

        public static Visibility GetActionsVisibility(DependencyObject o) => (Visibility)o.GetValue(ActionsVisibilityProperty);
        public static void SetActionsVisibility(DependencyObject o, Visibility v) => o.SetValue(ActionsVisibilityProperty, v);
    }

    /// <summary>One property/unit. Used by both the Overview and Properties pages.</summary>
    public class RentalProperty
    {
        private static readonly Dictionary<string, ImageSource> PhotoCache = new Dictionary<string, ImageSource>();
        private string _description = "";

        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public string Type { get; set; } = "";
        public string Floor { get; set; } = "";
        public int Floors { get; set; } = 1;                 // number of floors
        public string RoomType { get; set; } = "";
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int SqFt { get; set; }
        public decimal Price { get; set; }                  // per month (long term) or per night (short term)
        public string Status { get; set; } = "Available";   // Available, Reserved, Occupied, Draft
        public string Term { get; set; } = "Long term";     // Long term, Short term
        public int Popularity { get; set; }                 // renter inquiries, used for "Most Popular"
        public string ImagePath { get; set; } = "";
        public bool IsNew { get; set; }                     // added this session: shown first

        public bool IsDraft => Status == "Draft";

        // Short description. If none was written, a simple one is built from the other fields.
        public string Description
        {
            get => string.IsNullOrWhiteSpace(_description) ? AutoDescription : _description;
            set => _description = value ?? "";
        }

        private string AutoDescription =>
            (string.IsNullOrWhiteSpace(RoomType) ? "Rental unit" : RoomType) +
            (Floors > 1 ? $" with {Floors} floors" : "") +
            $". {Term} rental.";

        public string PriceUnit => Term == "Long term" ? "/ month" : "/ night";
        public string PriceText => "₱" + Price.ToString("N0", CultureInfo.InvariantCulture);
        public string PriceWithUnit => PriceText + " " + PriceUnit;
        public string SqFtText => SqFt > 0 ? SqFt.ToString("N0", CultureInfo.InvariantCulture) + " sqft" : "Size not set";
        public string BathroomsText => Bathrooms == 1 ? "1 Bathroom" : $"{Bathrooms} Bathrooms";

        public string BedroomsText
        {
            get
            {
                if (!string.IsNullOrEmpty(RoomType) && RoomType.StartsWith("Commercial", StringComparison.OrdinalIgnoreCase))
                    return "Commercial";
                return Bedrooms == 0 ? "Studio" : Bedrooms == 1 ? "1 Bedroom" : $"{Bedrooms} Bedrooms";
            }
        }

        // One line of facts, used in the text list view.
        public string Summary
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Location)) parts.Add(Location);
                parts.Add(BedroomsText);
                parts.Add(BathroomsText);
                if (SqFt > 0) parts.Add(SqFtText);
                parts.Add(Term);
                return string.Join(" · ", parts);
            }
        }

        // The photo shown on gallery cards. Loaded once per image path and reused.
        public ImageSource Photo
        {
            get
            {
                if (string.IsNullOrEmpty(ImagePath)) return null;

                if (!PhotoCache.TryGetValue(ImagePath, out ImageSource image))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(ImagePath, UriKind.RelativeOrAbsolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;   // don't keep the file locked
                        bitmap.DecodePixelWidth = 600;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        image = bitmap;
                    }
                    catch
                    {
                        image = null;   // missing file: the card just shows no photo
                    }
                    PhotoCache[ImagePath] = image;
                }
                return image;
            }
        }
    }

    /// <summary>One category block (title + its properties) shown on a page.</summary>
    public class PropertySection
    {
        public string Key { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public List<RentalProperty> Items { get; set; } = new List<RentalProperty>();
        public bool ShowDivider { get; set; }

        public Visibility DividerVisibility => ShowDivider ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>The single list of properties shared by the Overview and Properties pages.</summary>
    public static class PropertyStore
    {
        public const string DefaultImage = "pack://application:,,,/Resources/Images/houseImage1.jpg";

        public static ObservableCollection<RentalProperty> All { get; } = new ObservableCollection<RentalProperty>(Seed());

        public static void Add(RentalProperty property) => All.Insert(0, property);

        /// <summary>Deletes a property from the shared list. Returns false if it wasn't found.</summary>
        public static bool Remove(RentalProperty property) => All.Remove(property);

        /// <summary>
        /// Call after a property's fields were changed in place. Re-sets the item so
        /// CollectionChanged fires for anything listening to <see cref="All"/>.
        /// (If you add a database later, save the changes here.)
        /// </summary>
        public static void NotifyUpdated(RentalProperty property)
        {
            int index = All.IndexOf(property);
            if (index >= 0) All[index] = property;
        }

        // ---------- From the Add Property popup ----------

        public static RentalProperty FromForm(PropertyFormResult r)
        {
            if (r == null) return null;

            string primaryImage = (r.PhotoPaths != null && r.PhotoPaths.Count > 0)
                ? r.PhotoPaths[0]
                : DefaultImage;

            return new RentalProperty
            {
                Name = r.Name,
                Floors = r.NumberOfFloors > 0 ? r.NumberOfFloors : 1,
                Bedrooms = r.NumberOfRooms,
                Bathrooms = r.NumberOfBathrooms,
                SqFt = r.SizeUnit,
                Price = r.MonthlyRent,
                RoomType = r.PropertyType.ToString(),
                Type = r.PropertyType.ToString(),
                Status = string.IsNullOrWhiteSpace(r.Status) ? "Available" : r.Status,
                Term = "Long term",
                ImagePath = primaryImage,
                Description = r.Notes,
                IsNew = true
            };
        }

        // ---------- Categories for the Overview page ----------

        /// <param name="filter">"All", or one of: Popular, Affordable, Available, Reserved, Occupied, Long, Short, Drafts</param>
        /// <param name="maxPerSection">Cards/rows per section when "All" is selected</param>
        public static List<PropertySection> BuildSections(IEnumerable<RentalProperty> pool, string filter,
                                                          int maxPerSection, bool includeDrafts)
        {
            var all = pool.ToList();
            var live = all.Where(p => !p.IsDraft).ToList();

            var defs = new List<(string Key, string Title, string Subtitle, IEnumerable<RentalProperty> Items)>
            {
                ("Popular",    "Most Popular",      "Top picks ranked by renter inquiries",
                    live.OrderByDescending(p => p.IsNew).ThenByDescending(p => p.Popularity)),
                ("Affordable", "Most Affordable",   "Lowest monthly rent for long-term stays",
                    live.Where(p => p.Term == "Long term").OrderByDescending(p => p.IsNew).ThenBy(p => p.Price)),
                ("Available",  "Available Now",     "Occupancy status: ready for move-in",
                    live.Where(p => p.Status == "Available").OrderByDescending(p => p.IsNew).ThenByDescending(p => p.Popularity)),
                ("Reserved",   "Reserved",          "Occupancy status: booked, waiting for move-in",
                    live.Where(p => p.Status == "Reserved").OrderByDescending(p => p.IsNew).ThenByDescending(p => p.Popularity)),
                ("Occupied",   "Occupied",          "Occupancy status: currently rented",
                    live.Where(p => p.Status == "Occupied").OrderByDescending(p => p.IsNew).ThenByDescending(p => p.Popularity)),
                ("Long",       "Long-Term Rentals", "Leases billed monthly",
                    live.Where(p => p.Term == "Long term").OrderByDescending(p => p.IsNew).ThenByDescending(p => p.Popularity)),
                ("Short",      "Short-Term Stays",  "Nightly bookings",
                    live.Where(p => p.Term == "Short term").OrderByDescending(p => p.IsNew).ThenByDescending(p => p.Popularity)),
            };

            if (includeDrafts)
                defs.Add(("Drafts", "Drafts", "Saved but not published yet", all.Where(p => p.IsDraft)));

            bool showAll = filter == "All";
            var result = new List<PropertySection>();

            foreach (var d in defs)
            {
                if (!showAll && d.Key != filter) continue;

                var items = (showAll ? d.Items.Take(maxPerSection) : d.Items).ToList();
                if (items.Count == 0) continue;

                result.Add(new PropertySection
                {
                    Key = d.Key,
                    Title = d.Title,
                    Subtitle = d.Subtitle,
                    Items = items,
                    ShowDivider = result.Count > 0    // divider before every section except the first
                });
            }
            return result;
        }

        // ---------- Status-only grouping for the Properties page ----------

        /// <summary>Groups properties as Available, Reserved, Occupied. filter: "All" or one of those keys.</summary>
        public static List<PropertySection> BuildStatusSections(IEnumerable<RentalProperty> pool, string filter)
        {
            var live = pool.Where(p => !p.IsDraft).ToList();

            var defs = new[]
            {
                (Key: "Available", Title: "Available Now", Subtitle: "Ready for move-in"),
                (Key: "Reserved",  Title: "Reserved",      Subtitle: "Booked, waiting for move-in"),
                (Key: "Occupied",  Title: "Occupied",      Subtitle: "Currently rented"),
            };

            var result = new List<PropertySection>();
            foreach (var d in defs)
            {
                if (filter != "All" && filter != d.Key) continue;

                var items = live.Where(p => p.Status == d.Key)
                                .OrderByDescending(p => p.IsNew)
                                .ThenByDescending(p => p.Popularity)
                                .ToList();
                if (items.Count == 0) continue;

                result.Add(new PropertySection
                {
                    Key = d.Key,
                    Title = d.Title,
                    Subtitle = d.Subtitle,
                    Items = items,
                    ShowDivider = result.Count > 0
                });
            }
            return result;
        }

        // ---------- Sample data: replace with your real properties (database / DAO) ----------

        private static List<RentalProperty> Seed()
        {
            return new List<RentalProperty>
            {
                P("Sunrise Cottage",      "Garden District", "Cottage",    2, 1,  780,   9500, "Available", "Long term",  86, "Bright two-bedroom cottage with a sunny garden and a quiet street."),
                P("Maple Nook Cottage",   "Riverside",       "Cottage",    1, 1,  520,   7800, "Occupied",  "Long term",  64, "Cozy one-bedroom nook with a small porch near the riverbank."),
                P("Lakeview Cottage",     "Lakeside Row",    "Cottage",    2, 1,  860,   2400, "Available", "Short term", 92, "Lake-facing cottage with a deck, perfect for weekend stays."),
                P("Willow Cottage",       "Old Town",        "Cottage",    1, 1,  480,   1800, "Reserved",  "Short term", 71, "Compact cottage in the old town, steps from cafes and shops."),
                P("Cedar Cottage",        "Hillcrest",       "Cottage",    3, 2, 1100,  14500, "Occupied",  "Long term",  58, "Family-sized cottage with a large kitchen and room to grow."),

                P("Pine Ridge Chalet",    "Hillcrest",       "Chalet",     3, 2, 1450,  18000, "Occupied",  "Long term",  79, "Timber chalet on the ridge with a stone fireplace and valley views."),
                P("Alpine Chalet",        "Highland View",   "Chalet",     4, 3, 2050,   5200, "Available", "Short term", 95, "Spacious four-bedroom chalet with a hot tub and mountain views."),
                P("Birchwood Chalet",     "Highland View",   "Chalet",     2, 2, 1120,   3600, "Reserved",  "Short term", 83, "Warm birchwood interior with a wraparound balcony."),
                P("Stonebridge Chalet",   "Riverside",       "Chalet",     3, 2, 1380,  16500, "Available", "Long term",  67, "Stone-built chalet near the river with a private garden."),
                P("Fernhill Chalet",      "Hillcrest",       "Chalet",     2, 1,  960,  12000, "Reserved",  "Long term",  55, "Quiet hillside chalet with a cozy loft bedroom."),

                P("Skyline Penthouse",    "City Center",     "Penthouse",  3, 3, 2408,  45000, "Occupied",  "Long term",  97, "Top-floor penthouse with floor-to-ceiling windows and a private terrace."),
                P("Harbor View Penthouse","Harbor Row",      "Penthouse",  4, 3, 3050,   9500, "Available", "Short term", 90, "Luxury penthouse overlooking the harbor, ideal for short getaways."),
                P("Crown Penthouse",      "City Center",     "Penthouse",  2, 2, 1680,  32000, "Available", "Long term",  88, "Modern penthouse in the city center with a rooftop lounge."),
                P("Atrium Penthouse",     "Old Town",        "Penthouse",  3, 2, 2150,   6800, "Occupied",  "Short term", 74, "Light-filled penthouse built around a glass atrium."),
                P("Summit Penthouse",     "Harbor Row",      "Penthouse",  2, 2, 1520,  28500, "Reserved",  "Long term",  69, "Upper-level penthouse with skyline views and secure parking."),

                P("Meadow Farmhouse",     "Greenfield",      "Farmhouse",  4, 2, 2300,  22000, "Available", "Long term",  81, "Country farmhouse with a wide porch and open meadow views."),
                P("Orchard Farmhouse",    "Greenfield",      "Farmhouse",  3, 2, 1850,   4200, "Available", "Short term", 85, "Farmhouse surrounded by fruit trees, great for family retreats."),
                P("Brookside Farmhouse",  "Riverside",       "Farmhouse",  3, 2, 1700,  17500, "Occupied",  "Long term",  62, "Rustic farmhouse beside a stream with a spacious yard."),
                P("Harvest Farmhouse",    "Greenfield",      "Farmhouse",  5, 3, 2900,   4900, "Occupied",  "Short term", 77, "Large five-bedroom farmhouse for group stays and gatherings."),
                P("Barn Loft Farmhouse",  "Old Town",        "Farmhouse",  1, 1,  640,   8200, "Occupied",  "Long term",  52, "Converted barn loft with exposed beams and a compact kitchen."),
            };
        }

        private static RentalProperty P(string name, string location, string type, int beds, int baths, int sqft,
                                        decimal price, string status, string term, int popularity, string description)
        {
            return new RentalProperty
            {
                Name = name,
                Location = location,
                Type = type,
                RoomType = type,
                Bedrooms = beds,
                Bathrooms = baths,
                SqFt = sqft,
                Price = price,
                Status = status,
                Term = term,
                Popularity = popularity,
                ImagePath = DefaultImage,
                Description = description
            };
        }
    }
}