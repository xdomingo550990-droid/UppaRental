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
    public class InvoiceRow : INotifyPropertyChanged
    {
        private string _status = "Pending";

        public string InvoiceNo { get; set; } = "";
        public string Renter { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Period { get; set; } = "";
        public string DueDate { get; set; } = "";
        public decimal Amount { get; set; }
        public string AmountText => "₱" + Amount.ToString("N2", CultureInfo.InvariantCulture);

        // Paid, Pending, Overdue. Notifies the table so the badge updates instantly.
        public string Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Status)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    }

    public partial class BillingPage : UserControl
    {
        // SAMPLE DATA: replace with invoices loaded from your database / DAO.
        private readonly ObservableCollection<InvoiceRow> _invoices = new ObservableCollection<InvoiceRow>
        {
            new InvoiceRow { InvoiceNo = "INV-2001", Renter = "Juan Dela Cruz",  Unit = "Unit 101", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 12500, Status = "Paid" },
            new InvoiceRow { InvoiceNo = "INV-2002", Renter = "Maria Santos",    Unit = "Unit 204", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 9800,  Status = "Paid" },
            new InvoiceRow { InvoiceNo = "INV-2003", Renter = "Ana Reyes",       Unit = "Unit 105", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 12500, Status = "Paid" },
            new InvoiceRow { InvoiceNo = "INV-2004", Renter = "Mark Villanueva", Unit = "Unit 203", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 15000, Status = "Overdue" },
            new InvoiceRow { InvoiceNo = "INV-2005", Renter = "Carlo Mendoza",   Unit = "Unit 202", Period = "Oct 2026", DueDate = "Oct 15, 2026", Amount = 15000, Status = "Pending" },
            new InvoiceRow { InvoiceNo = "INV-1998", Renter = "Liza Garcia",     Unit = "Unit 301", Period = "Aug 2026", DueDate = "Aug 05, 2026", Amount = 9800,  Status = "Paid" },
        };

        private ICollectionView _view = null!;
        private string _statusFilter = "All";

        public BillingPage()
        {
            InitializeComponent();

            _view = CollectionViewSource.GetDefaultView(_invoices);
            _view.Filter = Matches;
            dgInvoices.ItemsSource = _view;

            UpdateSummary();
        }

        // ---------- Filtering ----------

        private bool Matches(object item)
        {
            var inv = (InvoiceRow)item;

            if (_statusFilter != "All" && inv.Status != _statusFilter) return false;

            var q = txtSearch?.Text?.Trim() ?? "";
            if (q.Length == 0) return true;

            return inv.InvoiceNo.Contains(q, StringComparison.OrdinalIgnoreCase)
                || inv.Renter.Contains(q, StringComparison.OrdinalIgnoreCase)
                || inv.Unit.Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _statusFilter = rb.Tag?.ToString() ?? "All";
            _view?.Refresh();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => _view?.Refresh();

        // ---------- Summary ----------

        private static string Peso(decimal value) => "₱" + value.ToString("N2", CultureInfo.InvariantCulture);

        private void UpdateSummary()
        {
            var paid = _invoices.Where(i => i.Status == "Paid").ToList();
            var pending = _invoices.Where(i => i.Status == "Pending").ToList();
            var overdue = _invoices.Where(i => i.Status == "Overdue").ToList();

            txtCollected.Text = Peso(paid.Sum(i => i.Amount));
            txtPending.Text = Peso(pending.Sum(i => i.Amount));
            txtOverdue.Text = Peso(overdue.Sum(i => i.Amount));

            txtCollectedNote.Text = $"{paid.Count} paid invoice(s)";
            txtPendingNote.Text = $"{pending.Count} awaiting payment";
            txtOverdueNote.Text = $"{overdue.Count} past due";

            rbAll.Content = $"All ({_invoices.Count})";
            rbPaid.Content = $"Paid ({paid.Count})";
            rbPendingFilter.Content = $"Pending ({pending.Count})";
            rbOverdue.Content = $"Overdue ({overdue.Count})";
        }

        // ---------- Buttons ----------

        // Works now: flips the invoice to Paid and refreshes the totals.
        private void MarkPaid_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InvoiceRow inv)
            {
                inv.Status = "Paid";
                _view.Refresh();
                UpdateSummary();
                // TODO: save the payment to your database here.
            }
        }

        // Placeholders: hook these up next.
        private void CreateInvoice_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("The Create Invoice form goes here.", "Create Invoice");
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InvoiceRow inv)
                MessageBox.Show($"{inv.InvoiceNo}\n{inv.Renter} · {inv.Unit}\n{inv.Period}: {inv.AmountText}\nStatus: {inv.Status}", "Invoice");
        }
    }
}