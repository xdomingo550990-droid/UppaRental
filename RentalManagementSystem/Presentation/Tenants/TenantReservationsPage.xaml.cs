using MySql.Data.MySqlClient;
using RentalManagementSystem.DAO;
using RentalManagementSystem.Model;
using RentalManagementSystem.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
public class PropertyStatusGroup
{
    public string StatusName { get; set; } = "";

    // Bindings expected by PropertyCardStyle.xaml
    public string Title => StatusName;
    public string Subtitle => $"{Items.Count} {(Items.Count == 1 ? "Property" : "Properties")}";
    public Visibility DividerVisibility => Visibility.Visible;
    public List<Property> Items { get; set; } = new();
}

namespace RentalManagementSystem.Presentation
{
    /// <summary>One of the tenant's reservations. Status: Confirmed, Pending (downpayment not paid yet) or Cancelled.</summary>
    public class MyReservationRow : INotifyPropertyChanged
    {
        public const decimal DownRate = 0.20m;
        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        private string _status = "Pending";

        public string ReservationNo { get; set; } = "";
        public string Unit { get; set; } = "";
        public string UnitType { get; set; } = "";
        public DateTime MoveIn { get; set; }
        public int Months { get; set; }
        public decimal MonthlyRate { get; set; }

        public string Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
        }

        // calculated
        public decimal TotalRent => MonthlyRate * Months;
        public decimal Downpayment => Math.Round(TotalRent * DownRate, 2);

        // shown in the table
        public string UnitLabel => $"{Unit} · {UnitType}";
        public string MoveInText => MoveIn.ToString("MMM d, yyyy", Us);
        public string TermText => Months == 1 ? "1 month" : $"{Months} months";
        public string TotalText => Peso(TotalRent);
        public string DownText => Peso(Downpayment);

        public static string Peso(decimal v) => "₱" + v.ToString("N2", Us);

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class TenantReservationsPage : UserControl
    {
        // Class-level declaration required by CollectionViewSource and query methods
        private readonly ObservableCollection<ReservationRow> _reservations = new ObservableCollection<ReservationRow>();
        private readonly ICollectionView _view;
        private User loggedInUser;
        private string _selectedStatusFilter = "All";
        private string _searchQuery = "";

        public TenantReservationsPage()
        {
            InitializeComponent();

            _view = CollectionViewSource.GetDefaultView(_reservations);
            _view.Filter = MatchesFilter;
            dgReservations.ItemsSource = _view;
            RefreshView();
            UpdateSummary();
        }

        // Chaining : this() executes InitializeComponent() and sets up _view
        public TenantReservationsPage(User loggedInUser) : this()
        {
            this.loggedInUser = loggedInUser;
            LoadReservationsFromDb();
        }

        private void LoadReservationsFromDb()
        {
            _reservations.Clear();
            if (loggedInUser == null) return;

            try
            {
                foreach (var row in ReservationDao.GetByUser(loggedInUser.getUserId()))
                    _reservations.Add(row);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading reservations: {ex.Message}");
                MessageBox.Show($"Could not load your reservations.\n\n{ex.Message}",
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            _view.Refresh();
            UpdateSummary();
        }

        public void ApplyGlobalSearch(string query)
        {
            _searchQuery = query ?? "";
            if (_view != null)
            {
                _view.Refresh();
                UpdateEmptyState();
            }
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                _selectedStatusFilter = rb.Tag.ToString();
                if (_view != null)
                {
                    _view.Refresh();
                    UpdateEmptyState();
                }
            }
        }

        private bool MatchesFilter(object item)
        {
            if (!(item is ReservationRow res)) return false;

            bool matchesStatus = string.Equals(_selectedStatusFilter, "All", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(res.Status, _selectedStatusFilter, StringComparison.OrdinalIgnoreCase);

            if (!matchesStatus) return false;

            if (string.IsNullOrWhiteSpace(_searchQuery)) return true;

            return (res.ReservationNo?.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (res.UnitLabel?.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (res.Status?.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        private void RefreshView()
        {
            // Filter_Checked fires once during InitializeComponent, before the view exists.
            if (_view == null) return;

            _view.Refresh();
            txtEmpty.Visibility = _view.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        // Pending -> Confirmed once the downpayment is paid (saved to reservations + payments).
        private void PayDown_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not ReservationRow r) return;
            if (loggedInUser == null || r.Status != "Pending") return;

            var answer = MessageBox.Show(
                $"Pay the downpayment of {r.DownText} for {r.UnitLabel}?",
                "Confirm downpayment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes) return;

            try
            {
                string receipt = ReservationDao.PayDownpayment(r.ReservationId, loggedInUser.getUserId(), r.DownpaymentAmount);
                LoadReservationsFromDb();
                MessageBox.Show($"Downpayment received (receipt {receipt}). {r.UnitLabel} is now reserved for you.", "My Reservations");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not process the downpayment.\n\n{ex.Message}",
                                "Payment Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateSummary()
        {
            int activeCount = _reservations.Count(r => r.Status == "Confirmed" || r.Status == "Pending");
            int pendingCount = _reservations.Count(r => r.Status == "Pending");
            decimal downPaid = _reservations.Where(r => r.Status == "Confirmed").Sum(r => r.DownpaymentAmount);
            int confirmedCount = _reservations.Count(r => r.Status == "Confirmed");
            
            var nextMove = _reservations
                .Where(r => (r.Status == "Confirmed" || r.Status == "Pending") && r.MoveInDate.HasValue)
                .OrderBy(r => r.MoveInDate)
                .FirstOrDefault();

            txtActive.Text = activeCount.ToString();
            txtActiveNote.Text = pendingCount > 0 ? $"{pendingCount} awaiting downpayment" : "All up to date";

            txtDownPaid.Text = $"₱{downPaid:N2}";
            txtDownPaidNote.Text = $"{confirmedCount} confirmed reservation(s)";

            if (nextMove != null && nextMove.MoveInDate.HasValue)
            {
                txtNextMoveIn.Text = nextMove.MoveInDate.Value.ToString("MMM dd, yyyy");
                txtNextMoveInNote.Text = $"{nextMove.UnitLabel} · {nextMove.TermMonths} months";
            }
            else
            {
                txtNextMoveIn.Text = "—";
                txtNextMoveInNote.Text = "No upcoming move-in";
            }

            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            bool hasItems = _view != null && !_view.IsEmpty;
            txtEmpty.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ReservationRow row)
            {
                MessageBox.Show($"Reservation #{row.ReservationNo}\nUnit: {row.UnitLabel}\nStatus: {row.Status}",
                                "Reservation Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not ReservationRow r) return;
            if (r.Status == "Cancelled") return;

            var answer = MessageBox.Show(
                $"Cancel reservation {r.ReservationNo} for {r.UnitLabel}?\nThe unit will become available to others again.",
                "Cancel reservation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes) return;

            try
            {
                ReservationDao.UpdateStatus(r.ReservationId, "Cancelled");
                LoadReservationsFromDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not cancel the reservation.\n\n{ex.Message}",
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }

    public class ReservationRow
    {
        public int ReservationId { get; set; }
        public string ReservationNo { get; set; }
        public string UnitLabel { get; set; }
        public DateTime? MoveInDate { get; set; }
        public int TermMonths { get; set; }
        public decimal TotalRent { get; set; }
        public decimal DownpaymentAmount { get; set; }
        public string Status { get; set; }

        public string MoveInText => MoveInDate.HasValue ? MoveInDate.Value.ToString("MMM dd, yyyy") : "—";
        public string TermText => $"{TermMonths} months";
        public string TotalText => $"₱{TotalRent:N2}";
        public string DownText => $"₱{DownpaymentAmount:N2}";
    }
}
