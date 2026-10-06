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
        // Only this tenant's reservations (replace with a database/DAO query for the logged-in tenant).
        private readonly ObservableCollection<MyReservationRow> _reservations = new ObservableCollection<MyReservationRow>
        {
            new MyReservationRow { ReservationNo = "R-1002", Unit = "Unit 203", UnitType = "2-Bedroom",    MonthlyRate = 20000, Months = 12, MoveIn = DateTime.Today.AddDays(10), Status = "Confirmed" },
            new MyReservationRow { ReservationNo = "R-1007", Unit = "Unit 301", UnitType = "Family Suite", MonthlyRate = 25000, Months = 6,  MoveIn = DateTime.Today.AddDays(45), Status = "Pending" },
            new MyReservationRow { ReservationNo = "R-0994", Unit = "Unit 102", UnitType = "Studio",       MonthlyRate = 10000, Months = 3,  MoveIn = DateTime.Today.AddDays(-20), Status = "Cancelled" },
        };

        private ICollectionView _view = null!;
        private string _statusFilter = "All";
        private string _globalSearchQuery = "";

        public TenantReservationsPage()
        {
            InitializeComponent();

            _view = CollectionViewSource.GetDefaultView(_reservations);
            _view.Filter = Matches;
            dgReservations.ItemsSource = _view;

            UpdateSummary();
        }

        // ---------- Global Search Hook (the dashboard's top search bar) ----------

        public void ApplyGlobalSearch(string query)
        {
            _globalSearchQuery = query?.Trim() ?? "";
            RefreshView();
        }

        // ---------- Filtering ----------

        private bool Matches(object item)
        {
            var r = (MyReservationRow)item;

            if (_statusFilter != "All" && r.Status != _statusFilter) return false;

            if (string.IsNullOrEmpty(_globalSearchQuery)) return true;

            return r.ReservationNo.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Unit.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.UnitType.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || r.Status.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _statusFilter = rb.Tag?.ToString() ?? "All";
            RefreshView();
        }

        private void RefreshView()
        {
            // Filter_Checked fires once during InitializeComponent, before the view exists.
            if (_view == null) return;

            _view.Refresh();
            txtEmpty.Visibility = _view.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------- Summary ----------

        private void UpdateSummary()
        {
            var confirmed = _reservations.Where(r => r.Status == "Confirmed").ToList();
            var pending = _reservations.Where(r => r.Status == "Pending").ToList();
            var cancelled = _reservations.Where(r => r.Status == "Cancelled").ToList();

            txtActive.Text = (confirmed.Count + pending.Count).ToString();
            txtActiveNote.Text = pending.Count == 0
                ? "All downpayments received"
                : $"{pending.Count} awaiting downpayment";

            txtDownPaid.Text = MyReservationRow.Peso(confirmed.Sum(r => r.Downpayment));
            txtDownPaidNote.Text = $"{confirmed.Count} confirmed reservation(s)";

            var next = confirmed.Where(r => r.MoveIn.Date >= DateTime.Today)
                                .OrderBy(r => r.MoveIn)
                                .FirstOrDefault();
            txtNextMoveIn.Text = next?.MoveInText ?? "—";
            txtNextMoveInNote.Text = next == null ? "No upcoming move-ins" : $"{next.Unit} · {next.TermText}";

            rbAll.Content = $"All ({_reservations.Count})";
            rbConfirmed.Content = $"Confirmed ({confirmed.Count})";
            rbPending.Content = $"Pending ({pending.Count})";
            rbCancelled.Content = $"Cancelled ({cancelled.Count})";
        }

        // ---------- Buttons ----------

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is MyReservationRow r)
            {
                MessageBox.Show(
                    $"{r.ReservationNo}\n{r.UnitLabel}\n\n" +
                    $"Move-in: {r.MoveInText}\nTerm: {r.TermText}\n" +
                    $"Monthly rent: {MyReservationRow.Peso(r.MonthlyRate)}\n" +
                    $"Total rent: {r.TotalText}\n" +
                    $"Downpayment (20%): {r.DownText}\n\nStatus: {r.Status}",
                    "Reservation details");
            }
        }

        // Pending -> Confirmed once the 20% downpayment is paid.
        private void PayDown_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not MyReservationRow r) return;

            var answer = MessageBox.Show(
                $"Pay the 20% downpayment of {r.DownText} for {r.Unit}?",
                "Confirm downpayment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes) return;

            // TODO: process the payment through your payment gateway / DAO here,
            // and only confirm the reservation once it succeeds.
            r.Status = "Confirmed";

            RefreshView();
            UpdateSummary();
            MessageBox.Show($"Downpayment received. {r.Unit} is now reserved for you.", "My Reservations");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not MyReservationRow r) return;

            var answer = MessageBox.Show(
                $"Cancel reservation {r.ReservationNo} for {r.Unit}?\nThe unit will become available to others again.",
                "Cancel reservation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes) return;

            // TODO: cancel `r` through your DAO / Service here (and free the unit on the landlord side).
            r.Status = "Cancelled";

            RefreshView();
            UpdateSummary();
        }
    }
}
