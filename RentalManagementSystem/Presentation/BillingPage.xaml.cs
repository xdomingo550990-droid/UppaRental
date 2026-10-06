using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RentalManagementSystem.Presentation
{
    public class RenterOption
    {
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal DefaultRent { get; set; }
    }

    public class InvoiceRow : INotifyPropertyChanged
    {
        private string _invoiceNo = "";
        private string _renter = "";
        private string _unit = "";
        private string _status = "Pending";
        private decimal _amount;
        private string _period = "";
        private string _dueDate = "";

        public string InvoiceNo
        {
            get => _invoiceNo;
            set { _invoiceNo = value; OnPropertyChanged(nameof(InvoiceNo)); }
        }

        public string Renter
        {
            get => _renter;
            set { _renter = value; OnPropertyChanged(nameof(Renter)); }
        }

        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(nameof(Unit)); }
        }

        public string Period
        {
            get => _period;
            set { _period = value; OnPropertyChanged(nameof(Period)); }
        }

        public string DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(nameof(DueDate)); }
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                _amount = value;
                OnPropertyChanged(nameof(Amount));
                OnPropertyChanged(nameof(AmountText));
            }
        }

        public string AmountText => "₱" + Amount.ToString("N2", CultureInfo.InvariantCulture);

        public string Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public partial class BillingPage : UserControl
    {
        private readonly ObservableCollection<InvoiceRow> _invoices = new ObservableCollection<InvoiceRow>
        {
            new InvoiceRow { InvoiceNo = "INV-2001", Renter = "Juan Dela Cruz",  Unit = "Unit 101", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 12500, Status = "Paid" },
            new InvoiceRow { InvoiceNo = "INV-2002", Renter = "Maria Santos",    Unit = "Unit 204", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 9800,  Status = "Paid" },
            new InvoiceRow { InvoiceNo = "INV-2003", Renter = "Ana Reyes",       Unit = "Unit 105", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 12500, Status = "Paid" },
            new InvoiceRow { InvoiceNo = "INV-2004", Renter = "Mark Villanueva", Unit = "Unit 203", Period = "Oct 2026", DueDate = "Oct 05, 2026", Amount = 15000, Status = "Overdue" },
            new InvoiceRow { InvoiceNo = "INV-2005", Renter = "Carlo Mendoza",   Unit = "Unit 202", Period = "Oct 2026", DueDate = "Oct 15, 2026", Amount = 15000, Status = "Pending" },
            new InvoiceRow { InvoiceNo = "INV-1998", Renter = "Liza Garcia",     Unit = "Unit 301", Period = "Aug 2026", DueDate = "Aug 05, 2026", Amount = 9800,  Status = "Paid" },
        };

        private readonly List<RenterOption> _renters = new List<RenterOption>
        {
            new RenterOption { Name = "Juan Dela Cruz", Unit = "Unit 101", DefaultRent = 12500 },
            new RenterOption { Name = "Maria Santos", Unit = "Unit 204", DefaultRent = 9800 },
            new RenterOption { Name = "Ana Reyes", Unit = "Unit 105", DefaultRent = 12500 },
            new RenterOption { Name = "Mark Villanueva", Unit = "Unit 203", DefaultRent = 15000 },
            new RenterOption { Name = "Carlo Mendoza", Unit = "Unit 202", DefaultRent = 15000 },
            new RenterOption { Name = "Liza Garcia", Unit = "Unit 301", DefaultRent = 9800 }
        };

        private ICollectionView _view = null!;
        private string _statusFilter = "All";
        private string _globalSearchQuery = "";
        private InvoiceRow? _selectedInvoice = null;

        public BillingPage()
        {
            InitializeComponent();

            _view = CollectionViewSource.GetDefaultView(_invoices);
            _view.Filter = Matches;
            DgInvoices.ItemsSource = _view;

            CmbRenter.ItemsSource = _renters;
            DpDueDate.SelectedDate = DateTime.Today.AddDays(7);
            TxtPeriod.Text = DateTime.Today.ToString("MMM yyyy");

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
                || inv.Renter.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || inv.Unit.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb) _statusFilter = rb.Tag?.ToString() ?? "All";
            _view?.Refresh();
        }

        private void UpdateSummary()
        {
            var paid = _invoices.Count(i => i.Status == "Paid");
            var pending = _invoices.Count(i => i.Status == "Pending");
            var overdue = _invoices.Count(i => i.Status == "Overdue");

            RbAll.Content = $"All ({_invoices.Count})";
            RbPaid.Content = $"Paid ({paid})";
            RbPendingFilter.Content = $"Pending ({pending})";
            RbOverdue.Content = $"Overdue ({overdue})";
        }

        // ---------- Inline Form Controls ----------

        private void CmbRenter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRenter.SelectedItem is RenterOption r)
            {
                TxtUnit.Text = r.Unit;
                if (_selectedInvoice == null)
                    TxtAmount.Text = r.DefaultRent.ToString("F2");
            }
        }

        private void DgInvoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgInvoices.SelectedItem is InvoiceRow inv)
            {
                _selectedInvoice = inv;
                BtnSave.Content = "✓ Update Invoice";

                CmbRenter.SelectedItem = _renters.FirstOrDefault(r => r.Name == inv.Renter);
                TxtUnit.Text = inv.Unit;
                TxtAmount.Text = inv.Amount.ToString("F2");
                TxtPeriod.Text = inv.Period;

                if (DateTime.TryParse(inv.DueDate, out var dt))
                    DpDueDate.SelectedDate = dt;

                foreach (ComboBoxItem item in CmbStatus.Items)
                {
                    if (item.Content?.ToString() == inv.Status)
                    {
                        CmbStatus.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void SaveInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (CmbRenter.SelectedItem is not RenterOption renter)
            {
                MessageBox.Show("Please select a renter.", "Validation Error");
                return;
            }

            if (!decimal.TryParse(TxtAmount.Text, out decimal amt))
            {
                MessageBox.Show("Please enter a valid amount.", "Validation Error");
                return;
            }

            string status = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pending";
            string dueDateStr = DpDueDate.SelectedDate?.ToString("MMM dd, yyyy") ?? DateTime.Today.ToString("MMM dd, yyyy");

            if (_selectedInvoice != null)
            {
                // UPDATE existing record
                _selectedInvoice.Renter = renter.Name;
                _selectedInvoice.Unit = renter.Unit;
                _selectedInvoice.Amount = amt;
                _selectedInvoice.Period = TxtPeriod.Text;
                _selectedInvoice.DueDate = dueDateStr;
                _selectedInvoice.Status = status;
            }
            else
            {
                // INSERT new record
                string newInvNo = "INV-" + (2000 + _invoices.Count + 1);
                _invoices.Insert(0, new InvoiceRow
                {
                    InvoiceNo = newInvNo,
                    Renter = renter.Name,
                    Unit = renter.Unit,
                    Amount = amt,
                    Period = TxtPeriod.Text,
                    DueDate = dueDateStr,
                    Status = status
                });
            }

            _view.Refresh();
            UpdateSummary();
            ClearForm();
        }

        private void ClearFields_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void DeleteInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (DgInvoices.SelectedItem is InvoiceRow inv)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {inv.InvoiceNo}?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _invoices.Remove(inv);
                    _view.Refresh();
                    UpdateSummary();
                    ClearForm();
                }
            }
            else
            {
                MessageBox.Show("Please select an invoice from the table to delete.", "No Selection");
            }
        }

        private void ClearForm()
        {
            _selectedInvoice = null;
            DgInvoices.SelectedItem = null;
            BtnSave.Content = "✓ Save Invoice";

            CmbRenter.SelectedIndex = -1;
            TxtUnit.Text = "";
            TxtAmount.Text = "";
            TxtPeriod.Text = DateTime.Today.ToString("MMM yyyy");
            DpDueDate.SelectedDate = DateTime.Today.AddDays(7);
            CmbStatus.SelectedIndex = 0;
        }

        // ---------- Row Actions ----------

        private void MarkPaid_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InvoiceRow inv)
            {
                inv.Status = "Paid";
                _view.Refresh();
                UpdateSummary();
            }
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InvoiceRow inv)
                MessageBox.Show($"{inv.InvoiceNo}\n{inv.Renter} · {inv.Unit}\n{inv.Period}: {inv.AmountText}\nStatus: {inv.Status}", "Invoice View");
        }
    }
}