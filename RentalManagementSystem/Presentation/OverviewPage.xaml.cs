#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RentalManagementSystem.Presentation
{
    public class OverviewProperty
    {
        private static readonly Dictionary<string, ImageSource> PhotoCache = new Dictionary<string, ImageSource>();

        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public string Type { get; set; } = "";             // Cottage, Chalet, Penthouse, Farmhouse
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int SqFt { get; set; }
        public decimal Price { get; set; }                  // per month (long term) or per night (short term)
        public string Status { get; set; } = "Available";   // Available, Reserved, Occupied
        public string Term { get; set; } = "Long term";     // Long term, Short term
        public int Popularity { get; set; }                 // renter inquiries, used for "Most Popular"
        public string ImagePath { get; set; } = "";

        public string PriceUnit => Term == "Long term" ? "/ month" : "/ night";
        public string PriceText => "₱" + Price.ToString("N0", CultureInfo.InvariantCulture);
        public string SqFtText => SqFt.ToString("N0", CultureInfo.InvariantCulture) + " sqft";
        public string BedroomsText => Bedrooms == 0 ? "Studio" : Bedrooms == 1 ? "1 Bedroom" : $"{Bedrooms} Bedrooms";
        public string BathroomsText => Bathrooms == 1 ? "1 Bathroom" : $"{Bathrooms} Bathrooms";

        // The photo shown on the card. Loaded once per image path and reused.
        public ImageSource Photo
        {
            get
            {
                if (string.IsNullOrEmpty(ImagePath)) return null;

                if (!PhotoCache.TryGetValue(ImagePath, out ImageSource image))
                {
                    try
                    {
                        var bitmap = new BitmapImage(new Uri(ImagePath, UriKind.RelativeOrAbsolute));
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

    public partial class OverviewPage : UserControl
    {
        private const string DefaultImage = "pack://application:,,,/Resources/Images/houseImage1.jpg";
        private const int MaxPerSection = 4;   // cards per section when "All" is selected

        private string _filter = "All";

        // SAMPLE DATA: replace with your real properties (database / DAO).
        // Pass an image path as the last argument to give a property its own photo.
        private readonly List<OverviewProperty> _all = new List<OverviewProperty>
        {
            // name,                  location,          type,         beds, baths, sqft,  price,  status,      term,         popularity
            P("Sunrise Cottage",      "Garden District", "Cottage",    2, 1,  780,   9500, "Available", "Long term",  86),
            P("Maple Nook Cottage",   "Riverside",       "Cottage",    1, 1,  520,   7800, "Occupied",  "Long term",  64),
            P("Lakeview Cottage",     "Lakeside Row",    "Cottage",    2, 1,  860,   2400, "Available", "Short term", 92),
            P("Willow Cottage",       "Old Town",        "Cottage",    1, 1,  480,   1800, "Reserved",  "Short term", 71),
            P("Cedar Cottage",        "Hillcrest",       "Cottage",    3, 2, 1100,  14500, "Occupied",  "Long term",  58),

            P("Pine Ridge Chalet",    "Hillcrest",       "Chalet",     3, 2, 1450,  18000, "Occupied",  "Long term",  79),
            P("Alpine Chalet",        "Highland View",   "Chalet",     4, 3, 2050,   5200, "Available", "Short term", 95),
            P("Birchwood Chalet",     "Highland View",   "Chalet",     2, 2, 1120,   3600, "Reserved",  "Short term", 83),
            P("Stonebridge Chalet",   "Riverside",       "Chalet",     3, 2, 1380,  16500, "Available", "Long term",  67),
            P("Fernhill Chalet",      "Hillcrest",       "Chalet",     2, 1,  960,  12000, "Reserved",  "Long term",  55),

            P("Skyline Penthouse",    "City Center",     "Penthouse",  3, 3, 2408,  45000, "Occupied",  "Long term",  97),
            P("Harbor View Penthouse","Harbor Row",      "Penthouse",  4, 3, 3050,   9500, "Available", "Short term", 90),
            P("Crown Penthouse",      "City Center",     "Penthouse",  2, 2, 1680,  32000, "Available", "Long term",  88),
            P("Atrium Penthouse",     "Old Town",        "Penthouse",  3, 2, 2150,   6800, "Occupied",  "Short term", 74),
            P("Summit Penthouse",     "Harbor Row",      "Penthouse",  2, 2, 1520,  28500, "Reserved",  "Long term",  69),

            P("Meadow Farmhouse",     "Greenfield",      "Farmhouse",  4, 2, 2300,  22000, "Available", "Long term",  81),
            P("Orchard Farmhouse",    "Greenfield",      "Farmhouse",  3, 2, 1850,   4200, "Available", "Short term", 85),
            P("Brookside Farmhouse",  "Riverside",       "Farmhouse",  3, 2, 1700,  17500, "Occupied",  "Long term",  62),
            P("Harvest Farmhouse",    "Greenfield",      "Farmhouse",  5, 3, 2900,   4900, "Occupied",  "Short term", 77),
            P("Barn Loft Farmhouse",  "Old Town",        "Farmhouse",  1, 1,  640,   8200, "Occupied",  "Long term",  52),
        };

        private static OverviewProperty P(string name, string location, string type, int beds, int baths,
                                          int sqft, decimal price, string status, string term,
                                          int popularity, string image = "")
        {
            return new OverviewProperty
            {
                Name = name,
                Location = location,
                Type = type,
                Bedrooms = beds,
                Bathrooms = baths,
                SqFt = sqft,
                Price = price,
                Status = status,
                Term = term,
                Popularity = popularity,
                ImagePath = string.IsNullOrEmpty(image) ? DefaultImage : image
            };
        }

        public OverviewPage()
        {
            InitializeComponent();
            LoadSections();
        }

        // ---------- Category chips ----------

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _filter = rb.Tag?.ToString() ?? "All";

            // Fires once during InitializeComponent, before the lists exist.
            if (icPopular == null) return;
            LoadSections();
        }

        // ---------- Sections ----------

        private void LoadSections()
        {
            var sections = new (string Key, StackPanel Section, ItemsControl List, IEnumerable<OverviewProperty> Items)[]
            {
                ("Popular",    secPopular,    icPopular,    _all.OrderByDescending(p => p.Popularity)),
                ("Affordable", secAffordable, icAffordable, _all.Where(p => p.Term == "Long term").OrderBy(p => p.Price)),
                ("Available",  secAvailable,  icAvailable,  _all.Where(p => p.Status == "Available").OrderByDescending(p => p.Popularity)),
                ("Reserved",   secReserved,   icReserved,   _all.Where(p => p.Status == "Reserved").OrderByDescending(p => p.Popularity)),
                ("Occupied",   secOccupied,   icOccupied,   _all.Where(p => p.Status == "Occupied").OrderByDescending(p => p.Popularity)),
                ("Long",       secLong,       icLong,       _all.Where(p => p.Term == "Long term").OrderByDescending(p => p.Popularity)),
                ("Short",      secShort,      icShort,      _all.Where(p => p.Term == "Short term").OrderByDescending(p => p.Popularity)),
            };

            bool showAll = _filter == "All";
            bool first = true;

            foreach (var (key, section, list, items) in sections)
            {
                // "All": 4 cards per section. One category chip: show every match in that category.
                var shown = (showAll ? items.Take(MaxPerSection) : items).ToList();
                bool visible = (showAll || key == _filter) && shown.Count > 0;

                list.ItemsSource = visible ? shown : null;
                section.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

                if (visible)
                {
                    // Child 0 is the divider: skip it above the first visible section
                    section.Children[0].Visibility = first ? Visibility.Collapsed : Visibility.Visible;
                    first = false;
                }
            }
        }

        // ---------- Buttons ----------

        // Placeholder: replace with your property details page or popup.
        private void ReadMore_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is OverviewProperty p)
            {
                MessageBox.Show(
                    $"{p.Name}\n{p.Location}\n\n{p.BedroomsText} · {p.BathroomsText} · {p.SqFtText}\n" +
                    $"{p.Term} · {p.Status}\n{p.PriceText} {p.PriceUnit}",
                    "Property details");
            }
        }
    }
}