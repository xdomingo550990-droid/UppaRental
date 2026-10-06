using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace RentalManagementSystem.Presentation
{
    /// <summary>One row in the table: either a renter (lease) or a reservation.</summary>
    public class RenterRow
    {
        public const decimal DownRate = 0.20m;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Contact { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string LeaseStart { get; set; } = "";
        public string LeaseEnd { get; set; } = "";
        public decimal MonthlyRent { get; set; }
        public string Status { get; set; } = "Active";   // Active, Pending, Past

        /// <summary>True when this row is a reservation (Pending until the renter moves in).</summary>
        public bool IsReservation { get; set; }

        /// <summary>Length of the lease in months (drives the reservation total and downpayment).</summary>
        public int Months { get; set; }

        public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name.Substring(0, 1).ToUpper();
        public string LeasePeriod => $"{LeaseStart} – {LeaseEnd}";
        public string RentText => "₱" + MonthlyRent.ToString("N0", Inv);

        // ---- reservation calculation: rent × months, 20% downpayment ----
        public decimal TotalRent => MonthlyRent * Months;
        public decimal Downpayment => Math.Round(TotalRent * DownRate, 2);
        public string TypeText => IsReservation ? "Reservation" : "Renter";
        public string DownText => IsReservation ? $"20% down: {Money(Downpayment)}" : "";

        public static string Money(decimal v) => "₱" + v.ToString("N2", Inv);

        /// <summary>Whole months covered from start to end (inclusive). Jan 15 – Jul 14 = 6.</summary>
        public static int MonthsBetween(DateTime start, DateTime end)
        {
            DateTime stop = end.Date.AddDays(1);
            int m = (stop.Year - start.Year) * 12 + stop.Month - start.Month;
            if (start.Date.AddMonths(m) > stop) m--;
            return Math.Max(m, 0);
        }
    }

    public partial class RentersPage : UserControl
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const string DateFormat = "MMM dd, yyyy";

        // Monthly rate of every unit. Used to pre-fill the rent when you reserve a unit.
        // TODO: load these from your DAO / Service (same source as the Properties page).
        private static readonly Dictionary<string, decimal> UnitRates = new()
        {
            ["Unit 101"] = 10000m, ["Unit 102"] = 10000m, ["Unit 105"] = 12500m,
            ["Unit 201"] = 15000m, ["Unit 202"] = 15000m,
            ["Unit 203"] = 20000m, ["Unit 204"] = 20000m,
            ["Unit 301"] = 25000m, ["Unit 302"] = 25000m,
            ["Unit 401"] = 30000m, ["Unit 402"] = 30000m,
        };
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

        private bool _ready;       // false while the page is still being built
        private int _suspend;      // > 0 while code (not the user) is filling the form

        private bool IsReservationMode => rbModeReservation.IsChecked == true;

        public RentersPage()
        {
            InitializeComponent();

            UnitCombo.ItemsSource = UnitRates.Keys.OrderBy(k => k).ToList();
            StatusCombo.ItemsSource = _statuses;

            SeedSampleReservations();

            _view = CollectionViewSource.GetDefaultView(_renters);
            _view.Filter = Matches;
            dgRenters.ItemsSource = _view;

            ClearForm();
            UpdateSummary();

            _ready = true;
            ApplyMode();
        }

        // Sample reservations (delete once your DAO supplies real ones)
        private void SeedSampleReservations()
        {
            AddSampleReservation("Paolo Ramirez", "0917 808 9090", "paolo.r@email.com", "Unit 102",  3,  6);
            AddSampleReservation("Grace Tan",     "0918 555 0142", "grace.t@email.com", "Unit 302", 10, 12);
            AddSampleReservation("Daniel Cruz",   "0917 123 8801", "daniel.c@email.com", "Unit 401", 20,  6);
        }

        private void AddSampleReservation(string name, string phone, string email, string unit, int moveInDays, int months)
        {
            DateTime start = DateTime.Today.AddDays(moveInDays);
            _renters.Add(new RenterRow
            {
                Name = name, Contact = phone, Email = email, Unit = unit,
                LeaseStart = start.ToString(DateFormat, Inv),
                LeaseEnd = start.AddMonths(months).AddDays(-1).ToString(DateFormat, Inv),
                MonthlyRent = UnitRates[unit], Months = months,
                Status = "Pending", IsReservation = true
            });
        }

        // ---------- Global Search Hook (called by DashboardPage's top search bar) ----------

        public void ApplyGlobalSearch(string query)
        {
            _globalSearchQuery = query?.Trim() ?? "";
            _view?.Refresh();
        }

        // ---------- Filtering ----------

        private bool Matches(object item)
        {
            var r = (RenterRow)item;

            if (_statusFilter == "Reservation") { if (!r.IsReservation) return false; }
            else if (_statusFilter != "All" && r.Status != _statusFilter) return false;

            if (string.IsNullOrEmpty(_globalSearchQuery)) return true;

            return r.Name.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Unit.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Contact.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Email.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Status.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.TypeText.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _statusFilter = rb.Tag?.ToString() ?? "All";
            _view?.Refresh();
        }

        // ---------- Filter pill counts ----------

        private void UpdateSummary()
        {
            rbAll.Content = $"All ({_renters.Count})";
            rbActive.Content = $"Active ({_renters.Count(r => r.Status == "Active")})";
            rbPending.Content = $"Pending ({_renters.Count(r => r.Status == "Pending")})";
            rbReservations.Content = $"Reservations ({_renters.Count(r => r.IsReservation)})";
            rbPast.Content = $"Past ({_renters.Count(r => r.Status == "Past")})";
        }

        // ---------- Entry type: New Renter | Reservation ----------

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            if (!_ready || _suspend > 0) return;

            bool res = IsReservationMode;

            if (_editing == null)
            {
                // Brand-new entry: switch the defaults to suit the chosen type
                _suspend++;
                try
                {
                    StatusCombo.SelectedItem = res ? "Pending" : "Active";
                    if (StartPicker.SelectedDate is DateTime s) EndPicker.SelectedDate = DefaultEnd(s);
                    if (res && UnitCombo.SelectedItem is string u && UnitRates.TryGetValue(u, out decimal rate))
                        RentBox.Text = rate.ToString("0.##", Inv);
                }
                finally { _suspend--; }
            }
            else if (!res && _editing.IsReservation && (StatusCombo.SelectedItem as string) == "Pending")
            {
                // Editing a reservation and switching to "New Renter" = the renter moved in
                StatusCombo.SelectedItem = "Active";
            }

            ApplyMode();
        }

        /// <summary>Updates every label and button that depends on renter / reservation / editing.</summary>
        private void ApplyMode()
        {
            bool res = IsReservationMode;
            bool edit = _editing != null;

            PanelTitle.Text = res ? "Reservation Details" : "Renter Details";
            PanelSubtitle.Text = res
                ? (edit ? "Editing a reservation. Switch to New Renter once the tenant moves in."
                        : "Reserve a unit. A 20% downpayment secures it until move-in.")
                : (edit ? "Editing a renter. Click Clear Fields to add a new one."
                        : "Add a new renter, or click a row in the table to edit it.");

            SaveText.Text = (edit ? "Update " : "Save ") + (res ? "Reservation" : "Renter");
            DeleteText.Text = res ? "Cancel Reservation" : "Delete Renter";
            TermLabel.Text = res ? "Move-in & End Date" : "Lease Term";
            RentLabel.Text = res ? "Monthly Rate (\u20B1)" : "Monthly Rent (\u20B1)";
            ReservationStrip.Visibility = res ? Visibility.Visible : Visibility.Collapsed;

            UpdateReservationSummary();
        }

        private static DateTime DefaultEnd(DateTime start, bool reservation) =>
            reservation ? start.AddMonths(6).AddDays(-1) : start.AddYears(1).AddDays(-1);

        private DateTime DefaultEnd(DateTime start) => DefaultEnd(start, IsReservationMode);

        // ---------- Live reservation calculation ----------
        // Reservation: total rent = monthly rate × months, downpayment = 20% of the total.

        private void UnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _suspend > 0) return;

            // For reservations the rent follows the unit's monthly rate
            if (IsReservationMode && UnitCombo.SelectedItem is string u && UnitRates.TryGetValue(u, out decimal rate))
                RentBox.Text = rate.ToString("0.##", Inv);

            UpdateReservationSummary();
        }

        private void Rent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ready) UpdateReservationSummary();
        }

        private void Date_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_ready) UpdateReservationSummary();
        }

        private void UpdateReservationSummary()
        {
            if (ReservationStrip == null || !IsReservationMode) return;

            decimal rate = TryParseRent(RentBox.Text, out decimal r) ? r : 0m;
            int months = 0;
            if (StartPicker.SelectedDate is DateTime s && EndPicker.SelectedDate is DateTime en && en > s)
                months = RenterRow.MonthsBetween(s, en);

            decimal total = rate * months;
            decimal down = Math.Round(total * RenterRow.DownRate, 2);

            ResTotalText.Text = months < 1
                ? "Pick the move-in and end dates to compute the total rent."
                : $"Total rent: {RenterRow.Money(total)}  ({months} {(months == 1 ? "month" : "months")} \u00D7 {RenterRow.Money(rate)})";
            ResDownText.Text = RenterRow.Money(down);
        }

        private static bool TryParseRent(string text, out decimal rent) =>
            decimal.TryParse(text.Replace("₱", "").Replace(",", "").Trim(), NumberStyles.Number, Inv, out rent);

        // ---------- Form: save / clear / delete ----------

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            bool res = IsReservationMode;

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
            { ShowMessage(res ? "Please pick the move-in and end dates." : "Please pick the lease start and end dates.", true); return; }

            DateTime start = StartPicker.SelectedDate.Value.Date;
            DateTime end = EndPicker.SelectedDate.Value.Date;
            if (end <= start) { ShowMessage("The end date must be after the start date.", true); return; }

            if (!TryParseRent(RentBox.Text, out decimal rent) || rent <= 0)
            { ShowMessage(res ? "Enter a valid monthly rate." : "Enter a valid monthly rent.", true); return; }

            if (status == null) { ShowMessage("Please choose a lease status.", true); return; }

            int months = RenterRow.MonthsBetween(start, end);
            bool newReservation = res && (_editing == null || !_editing.IsReservation);

            if (res)
            {
                if (newReservation && start < DateTime.Today)
                { ShowMessage("The move-in date can't be in the past.", true); return; }
                if (months < 1 || months > 60)
                { ShowMessage("A reservation must cover 1 to 60 months.", true); return; }
            }

            // one unit can't have two current renters / reservations
            if (status != "Past")
            {
                var clash = _renters.FirstOrDefault(r => r != _editing && r.Unit == unit && r.Status != "Past");
                if (clash != null)
                {
                    ShowMessage($"{unit} is already assigned to {clash.Name}.", true);
                    return;
                }
            }

            // a new reservation only counts once the 20% downpayment is received
            decimal down = Math.Round(rent * months * RenterRow.DownRate, 2);
            if (newReservation)
            {
                var answer = MessageBox.Show(
                    $"Confirm that the 20% downpayment of {RenterRow.Money(down)} for {unit} " +
                    $"has been received from {name}?",
                    "Confirm downpayment", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes) return;
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
            row.Months = months;
            row.Status = status;
            row.IsReservation = res;

            if (!isEdit) _renters.Add(row);

            // TODO: save `row` through your DAO / Service here (insert or update)

            _view.Refresh();
            UpdateSummary();
            ClearForm();

            if (newReservation) ShowMessage($"{unit} reserved for {row.Name}. Downpayment {RenterRow.Money(down)} recorded.", false);
            else ShowMessage(isEdit ? $"{row.Name} updated." : $"{row.Name} saved.", false);
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var target = dgRenters.SelectedItem as RenterRow ?? _editing;
            if (target == null)
            {
                ShowMessage("Select a renter or reservation in the table first.", true);
                return;
            }

            var answer = target.IsReservation
                ? MessageBox.Show($"Cancel the reservation of {target.Name} for {target.Unit}?\nThe unit will become available again.",
                                  "Cancel reservation", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                : MessageBox.Show($"Delete {target.Name} ({target.Unit})?", "Delete renter",
                                  MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            _renters.Remove(target);

            // TODO: delete / cancel `target` through your DAO / Service here

            UpdateSummary();
            ClearForm();
            ShowMessage(target.IsReservation ? $"{target.Name}'s reservation was cancelled." : $"{target.Name} deleted.", false);
        }

        /// <summary>Empties the form. Keeps the current type (renter / reservation) so you can add several in a row.</summary>
        private void ClearForm()
        {
            _suspend++;
            try
            {
                _editing = null;

                NameBox.Text = "";
                ContactBox.Text = "";
                EmailBox.Text = "";
                AddressBox.Text = "";
                RentBox.Text = "";
                UnitCombo.SelectedIndex = -1;
                StatusCombo.SelectedItem = IsReservationMode ? "Pending" : "Active";
                StartPicker.SelectedDate = DateTime.Today;
                EndPicker.SelectedDate = DefaultEnd(DateTime.Today);
            }
            finally { _suspend--; }

            dgRenters.UnselectAll();
            FormMessage.Visibility = Visibility.Collapsed;
            ApplyMode();
        }

        private void LoadForEdit(RenterRow r)
        {
            _suspend++;
            try
            {
                _editing = r;

                if (r.IsReservation) rbModeReservation.IsChecked = true;
                else rbModeRenter.IsChecked = true;

                NameBox.Text = r.Name;
                ContactBox.Text = r.Contact;
                EmailBox.Text = r.Email;
                AddressBox.Text = r.Address;
                UnitCombo.SelectedItem = r.Unit;
                StartPicker.SelectedDate = ParseDate(r.LeaseStart);
                EndPicker.SelectedDate = ParseDate(r.LeaseEnd);
                RentBox.Text = r.MonthlyRent.ToString("0.##", Inv);
                StatusCombo.SelectedItem = r.Status;
            }
            finally { _suspend--; }

            FormMessage.Visibility = Visibility.Collapsed;
            ApplyMode();
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

        // Clicking a row loads that renter / reservation into the form so it can be edited or deleted
        private void Renters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRenters.SelectedItem is RenterRow r) LoadForEdit(r);
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not RenterRow r) return;

            string extra = r.IsReservation
                ? $"\nTotal rent: {RenterRow.Money(r.TotalRent)} ({r.Months} months)\nDownpayment (20%): {RenterRow.Money(r.Downpayment)}"
                : "";

            MessageBox.Show(
                $"{r.Name}  ·  {r.TypeText}\n{r.Unit}\n{r.Contact}\n{r.Email}\n{r.Address}\nLease: {r.LeasePeriod}{extra}",
                r.IsReservation ? "Reservation details" : "Renter details");
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RenterRow r)
            {
                dgRenters.SelectedItem = r;   // selecting the row fills the form
                NameBox.Focus();
            }
        }
    }
}
