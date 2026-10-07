using RentalManagementSystem.Model;
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
    /// <summary>One entry in the tenant's payment history (a receipt).</summary>
    public class TenantPayment
    {
        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        public string ReceiptNo { get; set; } = "";
        public string InvoiceNo { get; set; } = "";
        public string Period { get; set; } = "";
        public DateTime DatePaid { get; set; }
        public string Method { get; set; } = "";
        public string Reference { get; set; } = "";
        public decimal Amount { get; set; }

        public string DatePaidText => DatePaid.ToString("MMM dd, yyyy", Us);
        public string AmountText => "₱" + Amount.ToString("N2", Us);
    }

    // Uses the InvoiceRow class already defined in BillingPage.xaml.cs (same namespace).
    public partial class TenantBillingPage : UserControl
    {
        private readonly ObservableCollection<InvoiceRow> _invoices = new ObservableCollection<InvoiceRow>();
        private readonly ObservableCollection<TenantPayment> _payments = new ObservableCollection<TenantPayment>();

        private int _nextReceipt = 3002;

        private ICollectionView _view = null!;
        private ICollectionView _historyView = null!;
        private string _statusFilter = "All";
        private string _globalSearchQuery = "";

        public User LoggedInUser { get; private set; }

        public TenantBillingPage()
        {
            InitializeComponent();

            _view = CollectionViewSource.GetDefaultView(_invoices);
            _view.Filter = Matches;
            dgInvoices.ItemsSource = _view;

            _historyView = CollectionViewSource.GetDefaultView(_payments);
            _historyView.Filter = MatchesPayment;
            dgHistory.ItemsSource = _historyView;

            LoadData();
        }

        // Chaining : this() ensures InitializeComponent() and controls load before assigning user
        public TenantBillingPage(User loggedInUser) : this()
        {
            LoggedInUser = loggedInUser;
            LoadData(); // Reload invoices and payments for the logged-in tenant
        }

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

        private void LoadData()
        {
            string tenantName = GetTenantName();

            // Replace with database / DAO query filtered by LoggedInUser.getUserId()
            _invoices.Clear();
            _invoices.Add(new InvoiceRow { InvoiceNo = "INV-2010", Renter = tenantName, Unit = "Unit 101", Period = "Nov 2026", DueDate = "Nov 05, 2026", Amount = 12500, Status = "Pending" });
            _invoices.Add(new InvoiceRow { InvoiceNo = "INV-2001", Renter = tenantName, Unit = "Unit 101", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 12500, Status = "Paid" });
            _invoices.Add(new InvoiceRow { InvoiceNo = "INV-1993", Renter = tenantName, Unit = "Unit 101", Period = "Sep 2026", DueDate = "Sep 05, 2026", Amount = 12500, Status = "Paid" });
            _invoices.Add(new InvoiceRow { InvoiceNo = "INV-1985", Renter = tenantName, Unit = "Unit 101", Period = "Aug 2026", DueDate = "Aug 05, 2026", Amount = 12500, Status = "Paid" });

            _payments.Clear();
            _payments.Add(new TenantPayment { ReceiptNo = "RCT-3001", InvoiceNo = "INV-2001", Period = "Oct 2026", DatePaid = new DateTime(2026, 10, 3), Method = "GCash", Reference = "GC-88421390", Amount = 12500 });
            _payments.Add(new TenantPayment { ReceiptNo = "RCT-2951", InvoiceNo = "INV-1993", Period = "Sep 2026", DatePaid = new DateTime(2026, 9, 4), Method = "Bank Transfer", Reference = "BT-55120934", Amount = 12500 });
            _payments.Add(new TenantPayment { ReceiptNo = "RCT-2890", InvoiceNo = "INV-1985", Period = "Aug 2026", DatePaid = new DateTime(2026, 8, 5), Method = "Cash", Reference = "—", Amount = 12500 });

            _view?.Refresh();
            _historyView?.Refresh();
            UpdateSummary();
        }

        // ---------- Global Search Hook ----------

        public void ApplyGlobalSearch(string query)
        {
            _globalSearchQuery = query?.Trim() ?? "";
            _view?.Refresh();
            _historyView?.Refresh();
        }

        // ---------- Tabs: Invoices | Payment History ----------

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (FilterBar == null || dgInvoices == null || dgHistory == null || txtHistoryTotal == null) return;

            bool showHistory = tabHistory.IsChecked == true;

            FilterBar.Visibility = showHistory ? Visibility.Collapsed : Visibility.Visible;
            dgInvoices.Visibility = showHistory ? Visibility.Collapsed : Visibility.Visible;
            dgHistory.Visibility = showHistory ? Visibility.Visible : Visibility.Collapsed;
            txtHistoryTotal.Visibility = showHistory ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------- Filtering ----------

        private bool Matches(object item)
        {
            var inv = (InvoiceRow)item;

            if (_statusFilter != "All" && inv.Status != _statusFilter) return false;

            if (string.IsNullOrEmpty(_globalSearchQuery)) return true;

            return inv.InvoiceNo.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || inv.Period.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || inv.Unit.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesPayment(object item)
        {
            var p = (TenantPayment)item;

            if (string.IsNullOrEmpty(_globalSearchQuery)) return true;

            return p.ReceiptNo.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || p.InvoiceNo.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || p.Period.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || p.Method.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || p.Reference.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase);
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
            tabHistory.Content = $"Payment History ({_payments.Count})";
            txtHistoryTotal.Text = $"{_payments.Count} payment(s) · Total paid {Peso(_payments.Sum(p => p.Amount))}";
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
            var result = MessageBox.Show(
                $"Pay {inv.AmountText} for {inv.InvoiceNo} ({inv.Period})?",
                "Confirm Payment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes) return;

            inv.Status = "Paid";

            var payment = new TenantPayment
            {
                ReceiptNo = "RCT-" + _nextReceipt++,
                InvoiceNo = inv.InvoiceNo,
                Period = inv.Period,
                DatePaid = DateTime.Today,
                Method = "Online payment",
                Reference = "REF-" + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                Amount = inv.Amount
            };
            _payments.Insert(0, payment);

            _view.Refresh();
            _historyView.Refresh();
            UpdateSummary();

            MessageBox.Show($"Payment recorded. Your receipt is {payment.ReceiptNo}.", "Billing and Payments");
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InvoiceRow inv)
                MessageBox.Show($"{inv.InvoiceNo}\n{inv.Unit}\n{inv.Period}: {inv.AmountText}\nDue: {inv.DueDate}\nStatus: {inv.Status}", "Invoice");
        }

        private void Receipt_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is TenantPayment p)
            {
                MessageBox.Show(
                    $"Receipt {p.ReceiptNo}\n\nInvoice: {p.InvoiceNo} ({p.Period})\nPaid on: {p.DatePaidText}\n" +
                    $"Method: {p.Method}\nReference: {p.Reference}\nAmount: {p.AmountText}\n\nPaid by: {GetTenantName()}",
                    "Payment receipt");
            }
        }
    }
}