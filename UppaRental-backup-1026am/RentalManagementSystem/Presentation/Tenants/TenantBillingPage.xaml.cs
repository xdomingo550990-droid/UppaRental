using MySql.Data.MySqlClient;
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

namespace RentalManagementSystem.Presentation
{
    /// <summary>
    /// Represents a payment receipt entry in the tenant's payment history.
    /// </summary>
    public class TenantPayment
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

        public string ReceiptNo { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public DateTime DatePaid { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        public string DatePaidText => DatePaid.ToString("MMM dd, yyyy", Culture);
        public string AmountText => "₱" + Amount.ToString("N2", Culture);
    }

    public partial class TenantBillingPage : UserControl
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

        private readonly ObservableCollection<InvoiceRow> _invoices = new();
        private readonly ObservableCollection<TenantPayment> _payments = new();

        private readonly ICollectionView _view;
        private readonly ICollectionView _historyView;

        private string _statusFilter = "All";
        private string _globalSearchQuery = string.Empty;

        public User? LoggedInUser { get; private set; }

        public TenantBillingPage()
        {
            // 1. Initialize collections and view wrappers first
            _view = CollectionViewSource.GetDefaultView(_invoices);
            _view.Filter = MatchesInvoice;

            _historyView = CollectionViewSource.GetDefaultView(_payments);
            _historyView.Filter = MatchesPayment;

            // 2. Initialize WPF XAML component (triggers RadioButton IsChecked events)
            InitializeComponent();

            // 3. Bind ItemSources
            dgInvoices.ItemsSource = _view;
            dgHistory.ItemsSource = _historyView;

            LoggedInUser = UserSession.CurrentUser;
            Loaded += TenantBillingPage_Loaded;
        }

        public TenantBillingPage(User loggedInUser) : this()
        {
            LoggedInUser = loggedInUser;
        }

        private void TenantBillingPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDataFromDatabase();
        }

        public string GetTenantName()
        {
            if (LoggedInUser != null)
            {
                string name = $"{LoggedInUser.getFirstName()} {LoggedInUser.getLastName()}".Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
            return "Tenant";
        }

        #region Database Data Loading

        /// <summary>
        /// Fetches real-time invoices and payment history records from MySQL for the logged-in user.
        /// </summary>
        public void LoadDataFromDatabase()
        {
            int userId = LoggedInUser?.getUserId() ?? LoggedInUser?.UserId ?? UserSession.UserId;

            if (userId == 0)
            {
                MessageBox.Show("Unable to load billing data. No user is logged in.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _invoices.Clear();
            _payments.Clear();

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // 1. Fetch Invoices from MySQL
                    string invoiceQuery = @"SELECT invoice_no, unit, period, due_date, amount, status 
                                            FROM invoices 
                                            WHERE user_id = @userId 
                                            ORDER BY due_date DESC";

                    using (var cmd = new MySqlCommand(invoiceQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _invoices.Add(new InvoiceRow
                                {
                                    InvoiceNo = reader.GetString("invoice_no"),
                                    Renter = GetTenantName(),
                                    Unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? "Unit N/A" : reader.GetString("unit"),
                                    Period = reader.GetString("period"),
                                    DueDate = reader.GetDateTime("due_date").ToString("MMM dd, yyyy", Culture),
                                    Amount = reader.GetDecimal("amount"),
                                    Status = reader.GetString("status")
                                });
                            }
                        }
                    }

                    // 2. Fetch Payment History Receipts from MySQL
                    string historyQuery = @"SELECT p.receipt_no, i.invoice_no, p.period, p.date_paid, p.method, p.reference, p.amount 
                                           FROM payments p
                                           INNER JOIN invoices i ON p.invoice_id = i.invoice_id
                                           WHERE p.user_id = @userId 
                                           ORDER BY p.date_paid DESC";

                    using (var cmd = new MySqlCommand(historyQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _payments.Add(new TenantPayment
                                {
                                    ReceiptNo = reader.GetString("receipt_no"),
                                    InvoiceNo = reader.GetString("invoice_no"),
                                    Period = reader.GetString("period"),
                                    DatePaid = reader.GetDateTime("date_paid"),
                                    Method = reader.GetString("method"),
                                    Reference = reader.IsDBNull(reader.GetOrdinal("reference")) ? "—" : reader.GetString("reference"),
                                    Amount = reader.GetDecimal("amount")
                                });
                            }
                        }
                    }
                }

                _view.Refresh();
                _historyView.Refresh();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading database records: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Search & Filtering

        public void ApplyGlobalSearch(string query)
        {
            _globalSearchQuery = query?.Trim() ?? string.Empty;
            _view.Refresh();
            _historyView.Refresh();
        }

        private bool MatchesInvoice(object item)
        {
            if (item is not InvoiceRow inv) return false;

            if (_statusFilter != "All" && !inv.Status.Equals(_statusFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrEmpty(_globalSearchQuery)) return true;

            return inv.InvoiceNo.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || inv.Period.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || inv.Unit.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesPayment(object item)
        {
            if (item is not TenantPayment p) return false;

            if (string.IsNullOrEmpty(_globalSearchQuery)) return true;

            return p.ReceiptNo.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || p.InvoiceNo.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || p.Period.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || p.Method.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase)
                || p.Reference.Contains(_globalSearchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                _statusFilter = rb.Tag?.ToString() ?? "All";
                _view?.Refresh(); // Safe null check prevents exception during InitializeComponent()
            }
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (FilterBar == null || dgInvoices == null || dgHistory == null || txtHistoryTotal == null) return;

            bool showHistory = tabHistory.IsChecked == true;

            FilterBar.Visibility = showHistory ? Visibility.Collapsed : Visibility.Visible;
            dgInvoices.Visibility = showHistory ? Visibility.Collapsed : Visibility.Visible;
            dgHistory.Visibility = showHistory ? Visibility.Visible : Visibility.Collapsed;
            txtHistoryTotal.Visibility = showHistory ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Summary Statistics Calculation

        private static string Peso(decimal value) => "₱" + value.ToString("N2", Culture);

        private void UpdateSummary()
        {
            if (txtPaid == null) return;

            var paid = _invoices.Where(i => i.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase)).ToList();
            var pending = _invoices.Where(i => i.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)).ToList();
            var overdue = _invoices.Where(i => i.Status.Equals("Overdue", StringComparison.OrdinalIgnoreCase)).ToList();

            // Stat Cards
            txtPaid.Text = Peso(paid.Sum(i => i.Amount));
            txtPending.Text = Peso(pending.Sum(i => i.Amount));
            txtOverdue.Text = Peso(overdue.Sum(i => i.Amount));

            txtPaidNote.Text = $"{paid.Count} paid invoice(s)";
            txtPendingNote.Text = $"{pending.Count} due soon";
            txtOverdueNote.Text = $"{overdue.Count} past due";

            // Filter Pill Counters
            rbAll.Content = $"All ({_invoices.Count})";
            rbPaid.Content = $"Paid ({paid.Count})";
            rbPendingFilter.Content = $"Pending ({pending.Count})";
            rbOverdue.Content = $"Overdue ({overdue.Count})";

            // Tab Counters
            tabInvoices.Content = $"Invoices ({_invoices.Count})";
            tabHistory.Content = $"Payment History ({_payments.Count})";
            txtHistoryTotal.Text = $"{_payments.Count} payment(s) · Total paid {Peso(_payments.Sum(p => p.Amount))}";
        }

        #endregion

        #region Database Transaction & Actions

        private void PayNextDue_Click(object sender, RoutedEventArgs e)
        {
            var next = _invoices.FirstOrDefault(i => i.Status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
                    ?? _invoices.LastOrDefault(i => i.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));

            if (next == null)
            {
                MessageBox.Show("You have no unpaid invoices. You're all caught up!", "Billing and Payments", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ExecutePaymentTransaction(next);
        }

        private void PayNow_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InvoiceRow inv)
            {
                ExecutePaymentTransaction(inv);
            }
        }

        private void ExecutePaymentTransaction(InvoiceRow inv)
        {
            int userId = LoggedInUser?.getUserId() ?? LoggedInUser?.UserId ?? UserSession.UserId;

            var result = MessageBox.Show(
                $"Pay {inv.AmountText} for {inv.InvoiceNo} ({inv.Period})?",
                "Confirm Payment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        // 1. Get invoice ID
                        int invoiceId = 0;
                        string getInvoiceIdQuery = "SELECT invoice_id FROM invoices WHERE invoice_no = @invoiceNo AND user_id = @userId LIMIT 1";
                        using (var cmd = new MySqlCommand(getInvoiceIdQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@invoiceNo", inv.InvoiceNo);
                            cmd.Parameters.AddWithValue("@userId", userId);
                            var obj = cmd.ExecuteScalar();
                            if (obj != null) invoiceId = Convert.ToInt32(obj);
                        }

                        // 2. Update Invoice status in MySQL
                        string updateInvoiceQuery = "UPDATE invoices SET status = 'Paid' WHERE invoice_no = @invoiceNo AND user_id = @userId";
                        using (var cmd = new MySqlCommand(updateInvoiceQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@invoiceNo", inv.InvoiceNo);
                            cmd.Parameters.AddWithValue("@userId", userId);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. Create and Insert Payment Record in MySQL
                        string receiptNo = "RCT-" + DateTime.Now.ToString("yyyyMMddHHmmss", Culture);
                        string refNo = "REF-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                        string insertPaymentQuery = @"INSERT INTO payments (receipt_no, invoice_id, user_id, period, date_paid, method, reference, amount)
                                                      VALUES (@receiptNo, @invoiceId, @userId, @period, @datePaid, 'Online Payment', @reference, @amount)";

                        using (var cmd = new MySqlCommand(insertPaymentQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@receiptNo", receiptNo);
                            cmd.Parameters.AddWithValue("@invoiceId", invoiceId);
                            cmd.Parameters.AddWithValue("@userId", userId);
                            cmd.Parameters.AddWithValue("@period", inv.Period);
                            cmd.Parameters.AddWithValue("@datePaid", DateTime.Now);
                            cmd.Parameters.AddWithValue("@reference", refNo);
                            cmd.Parameters.AddWithValue("@amount", inv.Amount);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        MessageBox.Show($"Payment recorded successfully!\nReceipt Number: {receiptNo}", "Payment Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                // Reload fresh state from Database
                LoadDataFromDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Payment failed to process: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is InvoiceRow inv)
            {
                MessageBox.Show($"Invoice Number: {inv.InvoiceNo}\nUnit: {inv.Unit}\nPeriod: {inv.Period}\nAmount: {inv.AmountText}\nDue Date: {inv.DueDate}\nStatus: {inv.Status}", "Invoice Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Receipt_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is TenantPayment p)
            {
                MessageBox.Show(
                    $"Receipt Number: {p.ReceiptNo}\n\nInvoice: {p.InvoiceNo} ({p.Period})\nDate Paid: {p.DatePaidText}\n" +
                    $"Method: {p.Method}\nReference: {p.Reference}\nAmount: {p.AmountText}\n\nPaid By: {GetTenantName()}",
                    "Payment Receipt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion
    }
}