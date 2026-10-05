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
    // Uses the InvoiceRow class already defined in BillingPage.xaml.cs (same namespace).
    public partial class TenantBillingPage : UserControl
    {
        private const string TenantName = "Juan Dela Cruz";

        // Only this tenant's invoices (replace with a database/DAO query filtered by the logged-in tenant).
        private readonly ObservableCollection<InvoiceRow> _invoices = new ObservableCollection<InvoiceRow>
        {
            new InvoiceRow { InvoiceNo = "INV-2010", Renter = TenantName, Unit = "Unit 101", Period = "Nov 2026", DueDate = "Nov 05, 2026", Amount = 12500, Status = "Pending" },
            new InvoiceRow { InvoiceNo = "INV-2001", Renter = TenantName, Unit = "Unit 101", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 12500, Status = "Paid" },
            new InvoiceRow { InvoiceNo = "INV-1993", Renter = TenantName, Unit = "Unit 101", Period = "Sep 2026", DueDate = "Sep 05, 2026", Amount = 12500, Status = "Paid" },
            new InvoiceRow { InvoiceNo = "INV-1985", Renter = TenantName, Unit = "Unit 101", Period = "Aug 2026", DueDate = "Aug 05, 2026", Amount = 12500, Status = "Paid" },
        };

        private ICollectionView _view = null!;
        private string _statusFilter = "All";
        private string _globalSearchQuery = "";

        public TenantBillingPage()
        {
            InitializeComponent();

            _view = CollectionViewSource.GetDefaultView(_invoices);
            _view.Filter = Matches;
            dgInvoices.ItemsSource = _view;

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
            var inv = (InvoiceRow)item;

            if (_statusFilter != "All" && inv.Status != _statusFilter) return false;

            if (string.IsNullOrEmpty(_globalSearchQuery)) return true;

            return inv.InvoiceNo.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || inv.Period.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || inv.Unit.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase);
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
        }

        // ---------- Buttons ----------

        // Pays the oldest unpaid invoice (overdue first, then pending).
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

            // TODO: process the payment through your payment gateway / DAO here,
            // and only mark the invoice as paid once it succeeds.
            inv.Status = "Paid";
            _view.Refresh();
            UpdateSummary();

            MessageBox.Show("Payment recorded. Thank you!", "Billing and Payments");
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InvoiceRow inv)
                MessageBox.Show($"{inv.InvoiceNo}\n{inv.Unit}\n{inv.Period}: {inv.AmountText}\nDue: {inv.DueDate}\nStatus: {inv.Status}", "Invoice");
        }
    }
}
