using System;
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
    }
}