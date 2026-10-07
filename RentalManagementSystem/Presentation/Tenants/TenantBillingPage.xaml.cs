using RentalManagementSystem.DAO;
using RentalManagementSystem.Model;
using RentalManagementSystem.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RentalManagementSystem.Presentation
{
    public partial class TenantBillingPage : UserControl
    {
        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        // Invoices remain managed locally or via an InvoiceViewModel
        private readonly ObservableCollection<InvoiceRow> _invoices = new();

        // Payment history is delegated to the view model
        public PaymentHistoryViewModel PaymentHistoryVm { get; } = new PaymentHistoryViewModel();

        private ICollectionView? _view;      // nullable: Filter_Checked can fire during InitializeComponent
        private string _statusFilter = "All";

        public User? LoggedInUser { get; }

        // Parameterless constructor kept for XAML / designer use
        public TenantBillingPage() : this(null) { }

        // Single real constructor: data loads once
        public TenantBillingPage(User? loggedInUser)
        {
            LoggedInUser = loggedInUser;

            InitializeComponent();
            DataContext = this;

            // The view model needs the user id so it can load THIS tenant's payments from the database
            PaymentHistoryVm.UserId = GetUserId();

            _view = CollectionViewSource.GetDefaultView(_invoices);
            _view.Filter = MatchesInvoice;
            dgInvoices.ItemsSource = _view;

            // NOTE: dgHistory.ItemsSource is bound in XAML to PaymentHistoryVm.FilteredItems.
            // Do NOT assign it here: FilteredItems is replaced on every filter pass.

            // Keep the tab header in sync with the view model
            PaymentHistoryVm.PropertyChanged += PaymentHistoryVm_PropertyChanged;

            _ = LoadDataAsync();
        }

        /// <summary>
        /// The one place that reads the logged-in user's id.
        /// TODO: if your User class uses a different member, change it here only
        /// (for example getUserId(), GetId() or the Id property).
        /// </summary>
        private int GetUserId() => LoggedInUser?.getUserId() ?? 0;

        public string GetTenantName()
        {
            if (LoggedInUser != null)
            {
                string name = $"{LoggedInUser.getFirstName()} {LoggedInUser.getLastName()}".Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
            return "Juan Dela Cruz";
        }

        private async Task LoadDataAsync()
        {
            try
            {
                string tenantName = GetTenantName();

                // 1. Payment history first (from the database), so we know which invoices are already paid
                await PaymentHistoryVm.LoadDataAsync();

                // 2. Invoices (still sample data; every invoice starts as Pending)
                _invoices.Clear();
                _invoices.Add(new InvoiceRow { InvoiceNo = "INV-2010", Renter = tenantName, Unit = "Unit 101", Period = "Nov 2026", DueDate = "Nov 05, 2026", Amount = 12500, Status = "Pending" });
                _invoices.Add(new InvoiceRow { InvoiceNo = "INV-2001", Renter = tenantName, Unit = "Unit 101", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 12500, Status = "Pending" });
                _invoices.Add(new InvoiceRow { InvoiceNo = "INV-1993", Renter = tenantName, Unit = "Unit 101", Period = "Sep 2026", DueDate = "Sep 05, 2026", Amount = 12500, Status = "Pending" });
                _invoices.Add(new InvoiceRow { InvoiceNo = "INV-1985", Renter = tenantName, Unit = "Unit 101", Period = "Aug 2026", DueDate = "Aug 05, 2026", Amount = 12500, Status = "Pending" });

                // 3. Any invoice that has a saved payment is Paid
                foreach (var inv in _invoices)
                {
                    bool hasPayment = PaymentHistoryVm.Items.Any(p =>
                        string.Equals(p.InvoiceNumber, inv.InvoiceNo, StringComparison.OrdinalIgnoreCase));

                    if (hasPayment) inv.Status = "Paid";
                }

                _view?.Refresh();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load billing data.\n\n{ex.Message}", "Billing and Payments",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PaymentHistoryVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PaymentHistoryViewModel.TotalCount) || string.IsNullOrEmpty(e.PropertyName))
                UpdateHistoryTabHeader();
        }

        private void UpdateHistoryTabHeader()
        {
            if (tabHistory == null) return;
            tabHistory.Content = $"Payment History ({PaymentHistoryVm.TotalCount})";
        }

        // ---------- Global search hook ----------

        public void ApplyGlobalSearch(string query)
        {
            // Set the query FIRST, then refresh (the invoice filter reads it)
            PaymentHistoryVm.SearchQuery = query ?? string.Empty;
            _view?.Refresh();
            UpdateSummary();
        }

        // ---------- Tabs: Invoices | Payment History ----------

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (FilterBar == null || dgInvoices == null || dgHistory == null || tabHistory == null) return;

            bool showHistory = tabHistory.IsChecked == true;

            FilterBar.Visibility = showHistory ? Visibility.Collapsed : Visibility.Visible;
            dgInvoices.Visibility = showHistory ? Visibility.Collapsed : Visibility.Visible;
            dgHistory.Visibility = showHistory ? Visibility.Visible : Visibility.Collapsed;
            // The history summary bar in XAML follows dgHistory's visibility via ElementName binding.
        }

        // ---------- Filtering ----------

        private bool MatchesInvoice(object item)
        {
            var inv = (InvoiceRow)item;

            if (_statusFilter != "All" && inv.Status != _statusFilter) return false;

            string q = PaymentHistoryVm.SearchQuery;
            if (string.IsNullOrEmpty(q)) return true;

            return inv.InvoiceNo.Contains(q, StringComparison.OrdinalIgnoreCase)
                || inv.Period.Contains(q, StringComparison.OrdinalIgnoreCase)
                || inv.Unit.Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _statusFilter = rb.Tag?.ToString() ?? "All";
            _view?.Refresh();
        }

        // ---------- Summary ----------

        private static string Peso(decimal value) => "₱" + value.ToString("N2", CultureInfo.InvariantCulture);

        private void UpdateSummary()
        {
            if (txtPaid == null) return;

            var paid = _invoices.Where(i => i.Status == "Paid").ToList();
            var pending = _invoices.Where(i => i.Status == "Pending").ToList();
            var overdue = _invoices.Where(i => i.Status == "Overdue").ToList();

            txtPaid.Text = Peso(paid.Sum(i => i.Amount));
            txtPending.Text = Peso(pending.Sum(i => i.Amount));
            txtOverdue.Text = Peso(overdue.Sum(i => i.Amount));

            txtPaidNote.Text = $"{paid.Count} paid invoice(s)";
            txtPendingNote.Text = $"{pending.Count} due soon";
            txtOverdueNote.Text = $"{overdue.Count} past due";

            rbAll.Content = $"All ({_invoices.Count})";
            rbPaid.Content = $"Paid ({paid.Count})";
            rbPendingFilter.Content = $"Pending ({pending.Count})";
            rbOverdue.Content = $"Overdue ({overdue.Count})";

            tabInvoices.Content = $"Invoices ({_invoices.Count})";
            UpdateHistoryTabHeader();

            // The history total text is bound in XAML to PaymentHistoryVm.HeaderSummaryText,
            // so it no longer needs to be set here.
        }

        // ---------- Buttons ----------

        private void PayNextDue_Click(object sender, RoutedEventArgs e)
        {
            var next = _invoices.FirstOrDefault(i => i.Status == "Overdue")
                    ?? _invoices.LastOrDefault(i => i.Status == "Pending");

            if (next == null)
            {
                MessageBox.Show("You have no unpaid invoices. You're all caught up!", "Billing and Payments");
                return;
            }

            Pay(next);
        }

        private void PayNow_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InvoiceRow inv)
                Pay(inv);
        }

        private void Pay(InvoiceRow inv)
        {
            if (inv.Status == "Paid")
            {
                MessageBox.Show($"{inv.InvoiceNo} is already paid.", "Billing and Payments");
                return;
            }

            int userId = GetUserId();
            if (userId <= 0)
            {
                MessageBox.Show("Could not identify the logged-in tenant. Please log in again.",
                                "Billing and Payments", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Pay {inv.AmountText} for {inv.InvoiceNo} ({inv.Period})?",
                "Confirm Payment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes) return;

            var payment = new Payment
            {
                UserId = userId,
                User = LoggedInUser,
                RenterName = GetTenantName(),
                InvoiceNumber = inv.InvoiceNo,
                Period = inv.Period,
                PaymentDate = DateTime.Today,
                PaymentMethod = "Online payment",
                ReferenceNumber = "REF-" + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                Amount = inv.Amount
            };

            try
            {
                // Saves to the database and sets PaymentId + ReceiptNumber (RCT-0001, ...)
                PaymentDao.Insert(payment);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Payment could not be saved.\n\n{ex.Message}", "Billing and Payments",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;   // invoice stays unpaid
            }

            inv.Status = "Paid";

            // Inserting triggers the view model's filter automatically (CollectionChanged),
            // which refreshes the grid, the total and the tab count.
            PaymentHistoryVm.Items.Insert(0, payment);

            _view?.Refresh();
            UpdateSummary();

            MessageBox.Show($"Payment recorded. Your receipt is {payment.ReceiptNumber}.", "Billing and Payments");
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InvoiceRow inv)
                MessageBox.Show($"{inv.InvoiceNo}\n{inv.Unit}\n{inv.Period}: {inv.AmountText}\nDue: {inv.DueDate}\nStatus: {inv.Status}", "Invoice");
        }

        private void Receipt_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is Payment p)
            {
                MessageBox.Show(
                    $"Receipt {p.ReceiptNumber}\n\nInvoice: {p.InvoiceNumber} ({p.Period})\n" +
                    $"Paid on: {p.PaymentDate.ToString("MMM dd, yyyy", Us)}\n" +
                    $"Method: {p.PaymentMethod}\nReference: {p.ReferenceNumber}\n" +
                    $"Amount: ₱{p.Amount.ToString("N2", Us)}\n\nPaid by: {GetTenantName()}",
                    "Payment receipt");
            }
        }
    }
}
