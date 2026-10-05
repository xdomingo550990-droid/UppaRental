using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RentalManagementSystem.Presentation
{
    public class RenterRow
    {
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Contact { get; set; } = "";
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
        private readonly ObservableCollection<RenterRow> _renters = new ObservableCollection<RenterRow>
        {
            new RenterRow { Name = "Juan Dela Cruz",   Unit = "Unit 101", Contact = "0917 123 4567", LeaseStart = "Jan 15, 2026", LeaseEnd = "Jan 14, 2027", MonthlyRent = 12500, Status = "Active" },
            new RenterRow { Name = "Maria Santos",     Unit = "Unit 204", Contact = "0918 765 4321", LeaseStart = "Mar 01, 2026", LeaseEnd = "Feb 28, 2027", MonthlyRent = 9800,  Status = "Active" },
            new RenterRow { Name = "Ana Reyes",        Unit = "Unit 105", Contact = "0922 555 0148", LeaseStart = "Jun 10, 2026", LeaseEnd = "Jun 09, 2027", MonthlyRent = 12500, Status = "Active" },
            new RenterRow { Name = "Mark Villanueva",  Unit = "Unit 203", Contact = "0916 880 3392", LeaseStart = "Feb 01, 2026", LeaseEnd = "Jan 31, 2027", MonthlyRent = 15000, Status = "Active" },
            new RenterRow { Name = "Carlo Mendoza",    Unit = "Unit 202", Contact = "0905 222 7781", LeaseStart = "Oct 15, 2026", LeaseEnd = "Oct 14, 2027", MonthlyRent = 15000, Status = "Pending" },
            new RenterRow { Name = "Liza Garcia",      Unit = "Unit 301", Contact = "0999 310 4426", LeaseStart = "Sep 01, 2025", LeaseEnd = "Aug 31, 2026", MonthlyRent = 9800,  Status = "Past" },
        };

        private ICollectionView _view = null!;
        private string _statusFilter = "All";
        private string _globalSearchQuery = "";

        public RentersPage()
        {
            InitializeComponent();

            _view = CollectionViewSource.GetDefaultView(_renters);
            _view.Filter = Matches;
            dgRenters.ItemsSource = _view;

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

        // ---------- Summary ----------

        private void UpdateSummary()
        {
            int total = _renters.Count;
            int active = _renters.Count(r => r.Status == "Active");
            int pending = _renters.Count(r => r.Status == "Pending");
            int past = _renters.Count(r => r.Status == "Past");

            txtTotal.Text = total.ToString();
            txtActive.Text = active.ToString();
            txtPending.Text = pending.ToString();

            rbAll.Content = $"All ({total})";
            rbActive.Content = $"Active ({active})";
            rbPending.Content = $"Pending ({pending})";
            rbPast.Content = $"Past ({past})";
        }

        // ---------- Buttons ----------

        private void AddRenter_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("The Add Renter form goes here.", "Add Renter");
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RenterRow r)
                MessageBox.Show($"{r.Name}\n{r.Unit}\n{r.Contact}\nLease: {r.LeasePeriod}", "Renter details");
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RenterRow r)
                MessageBox.Show($"Edit form for {r.Name} goes here.", "Edit Renter");
        }
    }
}