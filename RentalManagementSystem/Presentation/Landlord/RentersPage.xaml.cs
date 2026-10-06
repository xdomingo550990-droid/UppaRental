using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RentalManagementSystem.Presentation
{
    public class RenterRow
    {
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Contact { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string LeaseStart { get; set; } = "";
        public string LeaseEnd { get; set; } = "";
        public decimal MonthlyRent { get; set; }
        public string Status { get; set; } = "Active";   // Active, Pending, Past

        public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name.Substring(0, 1).ToUpper();
        public string LeasePeriod => $"{LeaseStart} – {LeaseEnd}";
        public string RentText => "₱" + MonthlyRent.ToString("N0", CultureInfo.InvariantCulture);
    }

#nullable disable
    /// <summary>One unit tile in the grid. Changing ReservedBy / IsSelected updates the screen instantly.</summary>
    public class ResUnitTile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ResUnitTile(string code, string typeName, decimal monthlyRate)
        {
            Code = code; TypeName = typeName; MonthlyRate = monthlyRate;
        }

        public string Code { get; }
        public string TypeName { get; }
        public decimal MonthlyRate { get; }

        private string _reservedBy;
        private bool _isSelected;

        public string ReservedBy
        {
            get => _reservedBy;
            set
            {
                _reservedBy = value;
                Raise(nameof(ReservedBy)); Raise(nameof(IsReserved)); Raise(nameof(ReservedText));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; Raise(nameof(IsSelected)); }
        }

        public bool IsReserved => !string.IsNullOrEmpty(ReservedBy);
        public string Title => $"Unit {Code}";
        public string RateText => $"{ResBooking.Peso}{MonthlyRate.ToString("N0", ResBooking.Us)}/mo";
        public string ReservedText => $"Reserved by: {ReservedBy}";

        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>One saved reservation (a row in the log).</summary>
    public class ResBooking
    {
        public const string Peso = "\u20B1";
        public const decimal DownRate = 0.20m;
        public static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        public string Id { get; set; } = "";
        public string RenterName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string UnitCode { get; set; } = "";
        public string UnitType { get; set; } = "";
        public decimal MonthlyRate { get; set; }
        public DateTime MoveIn { get; set; }
        public int Months { get; set; }

        // calculated
        public decimal TotalRent => MonthlyRate * Months;
        public decimal Downpayment => Math.Round(TotalRent * DownRate, 2);

        // shown in the log
        public string UnitText => $"Unit {UnitCode}";
        public string RenterSub => $"{Id} \u00B7 {Phone}";
        public string MoveInText => MoveIn.ToString("MMM d, yyyy", Us);
        public string TermText => Months == 1 ? "1 month" : $"{Months} months";
        public string TotalText => Money(TotalRent);
        public string DownText => Money(Downpayment);

        public static string Money(decimal v) => Peso + v.ToString("N2", Us);
    }
#nullable restore

    public partial class RentersPage : UserControl
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const string DateFormat = "MMM dd, yyyy";

        private readonly string[] _units =
            { "Unit 101", "Unit 102", "Unit 105", "Unit 201", "Unit 202", "Unit 203", "Unit 204", "Unit 301", "Unit 302", "Unit 401" };
        private readonly string[] _statuses = { "Active", "Pending", "Past" };

        private readonly ObservableCollection<RenterRow> _renters = new ObservableCollection<RenterRow>
        {
            new RenterRow { Name = "Juan Dela Cruz",   Unit = "Unit 101", Contact = "0917 123 4567", Email = "juan.dc@email.com",  Address = "Tagum City",  LeaseStart = "Jan 15, 2026", LeaseEnd = "Jan 14, 2027", MonthlyRent = 12500, Status = "Active" },
            new RenterRow { Name = "Maria Santos",     Unit = "Unit 204", Contact = "0918 765 4321", Email = "maria.s@email.com",  Address = "Davao City",  LeaseStart = "Mar 01, 2026", LeaseEnd = "Feb 28, 2027", MonthlyRent = 9800,  Status = "Active" },
            new RenterRow { Name = "Ana Reyes",        Unit = "Unit 105", Contact = "0922 555 0148", Email = "ana.reyes@email.com", Address = "Digos City", LeaseStart = "Jun 10, 2026", LeaseEnd = "Jun 09, 2027", MonthlyRent = 12500, Status = "Active" },
            new RenterRow { Name = "Mark Villanueva",  Unit = "Unit 203", Contact = "0916 880 3392", Email = "mark.v@email.com",   Address = "Davao City",  LeaseStart = "Feb 01, 2026", LeaseEnd = "Jan 31, 2027", MonthlyRent = 15000, Status = "Active" },
            new RenterRow { Name = "Carlo Mendoza",    Unit = "Unit 202", Contact = "0905 222 7781", Email = "carlo.m@email.com",  Address = "Mati City",   LeaseStart = "Oct 15, 2026", LeaseEnd = "Oct 14, 2027", MonthlyRent = 15000, Status = "Pending" },
            new RenterRow { Name = "Liza Garcia",      Unit = "Unit 301", Contact = "0999 310 4426", Email = "liza.g@email.com",   Address = "Panabo City", LeaseStart = "Sep 01, 2025", LeaseEnd = "Aug 31, 2026", MonthlyRent = 9800,  Status = "Past" },
        };

        private ICollectionView _view = null!;
        private RenterRow? _editing;
        private string _statusFilter = "All";
        private string _globalSearchQuery = "";

        public RentersPage()
        {
            InitializeComponent();

            UnitCombo.ItemsSource = _units;
            StatusCombo.ItemsSource = _statuses;

            _view = CollectionViewSource.GetDefaultView(_renters);
            _view.Filter = Matches;
            dgRenters.ItemsSource = _view;

            ClearForm();
            UpdateSummary();

            InitReservations();
        }

        // ---------- Tabs: Renters | Reservations ----------

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            // Fires once during InitializeComponent, before the views exist.
            if (RentersView == null || ReservationsView == null) return;

            bool showRenters = tabRenters.IsChecked == true;
            RentersView.Visibility = showRenters ? Visibility.Visible : Visibility.Collapsed;
            ReservationsView.Visibility = showRenters ? Visibility.Collapsed : Visibility.Visible;
        }

        // ---------- Global Search Hook ----------

        public void ApplyGlobalSearch(string query)
        {
            _globalSearchQuery = query?.Trim() ?? "";
            _view?.Refresh();
        }

        // ---------- Filtering ----------

        private bool Matches(object item)
        {
            var r = (RenterRow)item;

            if (_statusFilter != "All" && r.Status != _statusFilter) return false;

            if (string.IsNullOrEmpty(_globalSearchQuery)) return true;

            return r.Name.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Unit.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Contact.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _statusFilter = rb.Tag?.ToString() ?? "All";
            _view?.Refresh();
        }

        // ---------- Filter pill counts ----------

        private void UpdateSummary()
        {
            int total = _renters.Count;
            int active = _renters.Count(r => r.Status == "Active");
            int pending = _renters.Count(r => r.Status == "Pending");
            int past = _renters.Count(r => r.Status == "Past");

            rbAll.Content = $"All ({total})";
            rbActive.Content = $"Active ({active})";
            rbPending.Content = $"Pending ({pending})";
            rbPast.Content = $"Past ({past})";
        }

        // ---------- Form: save / clear / delete ----------

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string contact = ContactBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string? unit = UnitCombo.SelectedItem as string;
            string? status = StatusCombo.SelectedItem as string;

            // ---- validation ----
            if (name.Length == 0) { ShowMessage("Please enter the renter's name.", true); return; }
            if (contact.Length == 0) { ShowMessage("Please enter a contact number.", true); return; }
            if (email.Length > 0 && (!email.Contains('@') || !email.Contains('.')))
            { ShowMessage("That email address doesn't look right.", true); return; }
            if (unit == null) { ShowMessage("Please choose an assigned unit.", true); return; }
            if (!StartPicker.SelectedDate.HasValue || !EndPicker.SelectedDate.HasValue)
            { ShowMessage("Please pick the lease start and end dates.", true); return; }

            DateTime start = StartPicker.SelectedDate.Value.Date;
            DateTime end = EndPicker.SelectedDate.Value.Date;
            if (end <= start) { ShowMessage("The lease end date must be after the start date.", true); return; }

            string rentText = RentBox.Text.Replace("₱", "").Replace(",", "").Trim();
            if (!decimal.TryParse(rentText, NumberStyles.Number, Inv, out decimal rent) || rent <= 0)
            { ShowMessage("Enter a valid monthly rent.", true); return; }

            if (status == null) { ShowMessage("Please choose a lease status.", true); return; }

            // one unit can't have two current renters
            if (status != "Past")
            {
                var clash = _renters.FirstOrDefault(r => r != _editing && r.Unit == unit && r.Status != "Past");
                if (clash != null)
                {
                    ShowMessage($"{unit} is already assigned to {clash.Name}.", true);
                    return;
                }
            }

            // ---- save ----
            bool isEdit = _editing != null;
            var row = _editing ?? new RenterRow();

            row.Name = name;
            row.Contact = contact;
            row.Email = email;
            row.Address = AddressBox.Text.Trim();
            row.Unit = unit;
            row.LeaseStart = start.ToString(DateFormat, Inv);
            row.LeaseEnd = end.ToString(DateFormat, Inv);
            row.MonthlyRent = rent;
            row.Status = status;

            if (!isEdit) _renters.Add(row);

            // TODO: save `row` through your DAO / Service here (insert or update)

            _view.Refresh();
            UpdateSummary();
            ClearForm();
            ShowMessage(isEdit ? $"{row.Name} updated." : $"{row.Name} saved.", false);
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var target = dgRenters.SelectedItem as RenterRow ?? _editing;
            if (target == null)
            {
                ShowMessage("Select a renter in the table first.", true);
                return;
            }

            var answer = MessageBox.Show($"Delete {target.Name} ({target.Unit})?", "Delete renter",
                                         MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            _renters.Remove(target);

            // TODO: delete `target` through your DAO / Service here

            UpdateSummary();
            ClearForm();
            ShowMessage($"{target.Name} deleted.", false);
        }

        private void ClearForm()
        {
            _editing = null;
            SaveText.Text = "Save Renter";

            NameBox.Text = "";
            ContactBox.Text = "";
            EmailBox.Text = "";
            AddressBox.Text = "";
            RentBox.Text = "";
            UnitCombo.SelectedIndex = -1;
            StatusCombo.SelectedIndex = 0;
            StartPicker.SelectedDate = DateTime.Today;
            EndPicker.SelectedDate = DateTime.Today.AddYears(1).AddDays(-1);

            dgRenters.UnselectAll();
            FormMessage.Visibility = Visibility.Collapsed;
        }

        private void LoadForEdit(RenterRow r)
        {
            _editing = r;
            SaveText.Text = "Update Renter";

            NameBox.Text = r.Name;
            ContactBox.Text = r.Contact;
            EmailBox.Text = r.Email;
            AddressBox.Text = r.Address;
            UnitCombo.SelectedItem = r.Unit;
            StartPicker.SelectedDate = ParseDate(r.LeaseStart);
            EndPicker.SelectedDate = ParseDate(r.LeaseEnd);
            RentBox.Text = r.MonthlyRent.ToString("0.##", Inv);
            StatusCombo.SelectedItem = r.Status;

            FormMessage.Visibility = Visibility.Collapsed;
        }

        private static DateTime? ParseDate(string text) =>
            DateTime.TryParseExact(text, DateFormat, Inv, DateTimeStyles.None, out var d) ? d : (DateTime?)null;

        private void ShowMessage(string text, bool isError)
        {
            FormMessage.Text = text;
            FormMessage.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(isError ? "#FFB4A8" : "#BFE8CC")!;
            FormMessage.Visibility = Visibility.Visible;
        }

        // ---------- Table interaction ----------

        // Clicking a row loads that renter into the form so it can be edited or deleted
        private void Renters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRenters.SelectedItem is RenterRow r) LoadForEdit(r);
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RenterRow r)
                MessageBox.Show($"{r.Name}\n{r.Unit}\n{r.Contact}\n{r.Email}\n{r.Address}\nLease: {r.LeasePeriod}", "Renter details");
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RenterRow r)
            {
                dgRenters.SelectedItem = r;   // selecting the row fills the form
                NameBox.Focus();
            }
        }

        // =====================================================================
        //  RESERVATIONS (merged from the old ReservationsPage)
        //  Simple in-memory data; later move it to your DAO / Service layer.
        // =====================================================================
#nullable disable
        private readonly List<ResUnitTile> _unitTiles = new()
        {
            new ResUnitTile("101", "Studio",       10000m),
            new ResUnitTile("102", "Studio",       10000m),
            new ResUnitTile("201", "1-Bedroom",    15000m),
            new ResUnitTile("202", "1-Bedroom",    15000m),
            new ResUnitTile("203", "2-Bedroom",    20000m),
            new ResUnitTile("204", "2-Bedroom",    20000m),
            new ResUnitTile("301", "Family Suite", 25000m),
            new ResUnitTile("302", "Family Suite", 25000m),
            new ResUnitTile("401", "Penthouse",    30000m),
            new ResUnitTile("402", "Penthouse",    30000m),
        };

        private readonly ObservableCollection<ResBooking> _bookings = new();
        private ResUnitTile _selectedTile;
        private int _nextBookingId = 1001;
        private bool _resReady;

        private void InitReservations()
        {
            ResUnitList.ItemsSource = _unitTiles;
            ResBookingGrid.ItemsSource = _bookings;

            SeedSampleData();
            ResetResForm();

            _resReady = true;
            UpdateResSummary();
            UpdateLog();
        }

        // -----------------------------------------------------------------
        //  Sample data (delete this once your DAO supplies real reservations)
        // -----------------------------------------------------------------
        private void SeedSampleData()
        {
            Seed("Maria Santos",    "+63 917 123 4567", "maria.santos@email.com", "101",  3,  6);
            Seed("Juan Dela Cruz",  "+63 918 555 0142", "juan.dc@email.com",      "203", 10, 12);
            Seed("Mark Villanueva", "+63 917 808 9090", "mark.v@email.com",       "401", 20, 12);
        }

        private void Seed(string name, string phone, string email, string unitCode, int moveInDays, int months)
        {
            var unit = _unitTiles.First(u => u.Code == unitCode);
            _bookings.Add(new ResBooking
            {
                Id = "R-" + _nextBookingId++,
                RenterName = name, Phone = phone, Email = email,
                UnitCode = unit.Code, UnitType = unit.TypeName, MonthlyRate = unit.MonthlyRate,
                MoveIn = DateTime.Today.AddDays(moveInDays), Months = months
            });
            unit.ReservedBy = name;
        }

        // -----------------------------------------------------------------
        //  Unit grid: only available units can be clicked
        // -----------------------------------------------------------------
        private void ResUnit_Click(object sender, MouseButtonEventArgs e)
        {
            var tile = (sender as FrameworkElement)?.DataContext as ResUnitTile;
            if (tile == null || tile.IsReserved) return;

            if (_selectedTile == tile)                       // click again = unselect
            {
                tile.IsSelected = false;
                _selectedTile = null;
            }
            else
            {
                if (_selectedTile != null) _selectedTile.IsSelected = false;
                tile.IsSelected = true;
                _selectedTile = tile;
            }

            ResFormMessage.Text = "";
            UpdateResSummary();
        }

        // -----------------------------------------------------------------
        //  Live calculation: total rent and 20% downpayment
        // -----------------------------------------------------------------
        private void ResDuration_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !e.Text.All(char.IsDigit);                  // numbers only

        private void ResDuration_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_resReady) UpdateResSummary();
        }

        private int ParseMonths() => int.TryParse(ResDurationBox.Text.Trim(), out int m) ? m : 0;

        private void UpdateResSummary()
        {
            int months = Math.Max(0, ParseMonths());
            decimal rate = _selectedTile?.MonthlyRate ?? 0m;
            decimal total = rate * months;
            decimal down = Math.Round(total * ResBooking.DownRate, 2);

            ResSumRate.Text = _selectedTile == null ? "\u2014" : ResBooking.Money(rate) + " / month";
            ResSumTotal.Text = ResBooking.Money(total);
            ResSumDown.Text = ResBooking.Money(down);

            if (_selectedTile == null)
            {
                ResSelectedUnitText.Text = "No unit selected \u2013 click an available unit on the left.";
                ResBannerText.Text = "Select an available unit to see the required 20% downpayment.";
            }
            else
            {
                ResSelectedUnitText.Text = $"{_selectedTile.Title} \u00B7 {_selectedTile.TypeName} \u00B7 {_selectedTile.RateText}";
                ResBannerText.Text = months < 1
                    ? "Enter the rental duration (in months) to compute the downpayment."
                    : $"To reserve this unit, an initial 20% downpayment of {ResBooking.Money(down)} is required.";
            }
        }

        // -----------------------------------------------------------------
        //  Reserve Unit
        // -----------------------------------------------------------------
        private void ResReserve_Click(object sender, RoutedEventArgs e)
        {
            string name = ResNameBox.Text.Trim();
            string phone = ResPhoneBox.Text.Trim();
            string email = ResEmailBox.Text.Trim();
            int months = ParseMonths();

            // ---- validation ----
            if (_selectedTile == null) { ShowResMessage("Please click an available unit first.", true); return; }
            if (name.Length == 0) { ShowResMessage("Please enter the renter's full name.", true); return; }
            if (phone.Length == 0) { ShowResMessage("Please enter a contact number.", true); return; }
            if (email.Length > 0 && (!email.Contains("@") || !email.Contains(".")))
            { ShowResMessage("That email address doesn't look right.", true); return; }
            if (!ResMoveInPicker.SelectedDate.HasValue) { ShowResMessage("Please pick a move-in date.", true); return; }
            if (ResMoveInPicker.SelectedDate.Value.Date < DateTime.Today)
            { ShowResMessage("The move-in date can't be in the past.", true); return; }
            if (months < 1 || months > 60) { ShowResMessage("Duration must be between 1 and 60 months.", true); return; }

            var unit = _selectedTile;
            decimal total = unit.MonthlyRate * months;
            decimal down = Math.Round(total * ResBooking.DownRate, 2);

            // The unit is only blocked once the 20% downpayment is received
            var answer = MessageBox.Show(
                $"Confirm that the 20% downpayment of {ResBooking.Money(down)} for {unit.Title} " +
                $"has been received from {name}?",
                "Confirm downpayment", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            // ---- save ----
            var booking = new ResBooking
            {
                Id = "R-" + _nextBookingId++,
                RenterName = name, Phone = phone, Email = email,
                UnitCode = unit.Code, UnitType = unit.TypeName, MonthlyRate = unit.MonthlyRate,
                MoveIn = ResMoveInPicker.SelectedDate.Value.Date, Months = months
            };
            _bookings.Insert(0, booking);

            // TODO: save `booking` through your DAO / Service here

            unit.IsSelected = false;
            unit.ReservedBy = name;          // tile turns gray, unclickable, "Reserved by: name"
            _selectedTile = null;

            ResetResForm();
            UpdateResSummary();
            UpdateLog();
            ShowResMessage($"{unit.Title} reserved for {name}. Downpayment {ResBooking.Money(down)} recorded.", false);
        }

        // -----------------------------------------------------------------
        //  Cancel a reservation (frees the unit again)
        // -----------------------------------------------------------------
        private void ResCancelRow_Click(object sender, RoutedEventArgs e)
        {
            var booking = (sender as FrameworkElement)?.DataContext as ResBooking;
            if (booking == null) return;

            var answer = MessageBox.Show(
                $"Cancel reservation {booking.Id} for {booking.RenterName} ({booking.UnitText})?\n" +
                "The unit will become available again.",
                "Cancel reservation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            _bookings.Remove(booking);

            var unit = _unitTiles.FirstOrDefault(u => u.Code == booking.UnitCode);
            if (unit != null) unit.ReservedBy = null;   // tile becomes clickable again

            // TODO: delete / cancel `booking` through your DAO / Service here
            UpdateLog();
        }

        // -----------------------------------------------------------------
        //  Small helpers
        // -----------------------------------------------------------------
        private void ResetResForm()
        {
            ResNameBox.Text = "";
            ResPhoneBox.Text = "";
            ResEmailBox.Text = "";
            ResMoveInPicker.SelectedDate = DateTime.Today;
            ResDurationBox.Text = "6";
        }

        private void UpdateLog()
        {
            int n = _bookings.Count;
            ResLogCountText.Text = n == 1 ? "1 active reservation" : $"{n} active reservations";
            ResEmptyText.Visibility = n == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowResMessage(string text, bool isError)
        {
            ResFormMessage.Text = text;
            ResFormMessage.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(isError ? "#C0392B" : "#1E6B3B");
        }
#nullable restore
    }
}
