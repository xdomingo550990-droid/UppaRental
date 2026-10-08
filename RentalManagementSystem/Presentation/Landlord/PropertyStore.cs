#nullable disable
using RentalManagementSystem.DAO;
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

        /// <summary>Database primary key (0 when not yet persisted / sample data).</summary>
        public int DbId { get; set; }

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

        // Loaded from the database.
        public static ObservableCollection<RentalProperty> All { get; } =
            new ObservableCollection<RentalProperty>(Load());

        public static void Add(RentalProperty property)
        {
            try
            {
                var model = ToModel(property);
                PropertyDao.Insert(model);
                property.DbId = model.PropertyId;
                All.Insert(0, property);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the property.\n\n{ex.Message}",
                                "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>Deletes a property from the shared list. Returns false if it wasn't found.</summary>
        public static bool Remove(RentalProperty property)
        {
            bool removed = All.Remove(property);

            if (removed && property.DbId > 0)
            {
                try { PropertyDao.Delete(property.DbId); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Property delete failed: {ex.Message}"); }
            }
            return removed;
        }

        /// <summary>
        /// Call after a property's fields were changed in place. Re-sets the item so
        /// CollectionChanged fires for anything listening to <see cref="All"/>,
        /// and persists the change to the database.
        /// </summary>
        public static void NotifyUpdated(RentalProperty property)
        {
            int index = All.IndexOf(property);
            if (index >= 0) All[index] = property;

            if (property.DbId > 0)
            {
                try { PropertyDao.Update(ToModel(property)); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Property update failed: {ex.Message}"); }
            }
        }

        // ---------- Database load / mapping ----------

        private static List<RentalProperty> Load()
        {
            try
            {
                return PropertyDao.GetAll().Select(FromModel).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Property load failed: {ex.Message}");
                MessageBox.Show($"Could not load properties from the database.\n\n{ex.Message}",
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<RentalProperty>();
            }
        }

        private static RentalProperty FromModel(Property p)
        {
            string primaryImage = (p.PhotoPaths != null && p.PhotoPaths.Count > 0)
                ? p.PhotoPaths[0]
                : DefaultImage;

            return new RentalProperty
            {
                DbId = p.PropertyId,
                Name = p.Name,
                Location = p.Location,
                Type = p.PropertyType.ToString(),
                RoomType = p.PropertyType.ToString(),
                Floors = p.NumberOfFloors > 0 ? p.NumberOfFloors : 1,
                Bedrooms = p.NumberOfRooms,
                Bathrooms = p.NumberofBathrooms,
                SqFt = p.SizeUnit,
                Price = p.MonthlyRent,
                Status = string.IsNullOrWhiteSpace(p.Status) ? "Available" : p.Status,
                Term = "Long term",
                Popularity = p.InquiryCount,
                ImagePath = primaryImage,
                Description = string.IsNullOrWhiteSpace(p.Description) ? p.Notes : p.Description,
                IsNew = false
            };
        }

        private static Property ToModel(RentalProperty r)
        {
            if (!Enum.TryParse(r.Type, true, out PropertyType type))
                type = PropertyType.Studio;

            return new Property
            {
                PropertyId = r.DbId,
                Name = r.Name,
                Location = r.Location,
                PropertyType = type,
                NumberOfFloors = r.Floors,
                NumberOfRooms = r.Bedrooms,
                NumberofBathrooms = r.Bathrooms,
                SizeUnit = r.SqFt,
                MonthlyRent = (int)r.Price,
                Status = r.Status,
                Description = r.Description,
                Notes = r.Description,
                PhotoPaths = new List<string> { r.ImagePath },
                InquiryCount = r.Popularity
            };
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
    }
}