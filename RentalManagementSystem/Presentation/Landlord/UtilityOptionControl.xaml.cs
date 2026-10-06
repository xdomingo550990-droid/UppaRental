using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RentalManagementSystem.Presentation
{
    public partial class UtilityOptionControl : UserControl
    {
        private static readonly (UtilityBilling Value, string Text)[] Modes =
        {
            (UtilityBilling.TenantPaysProvider, "Tenant pays provider directly"),
            (UtilityBilling.FixedMonthlyFee,    "Fixed monthly fee"),
            (UtilityBilling.SplitAmongTenants,  "Split equally among tenants"),
            (UtilityBilling.PerPersonRate,      "Per-person rate"),
            (UtilityBilling.SubMetered,         "Sub-metered (per kWh)")
        };

        private readonly Dictionary<UtilityBilling, ComboBoxItem> _items = new();

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(UtilityOptionControl),
                new PropertyMetadata("", (d, e) => ((UtilityOptionControl)d).chkIncluded.Content = e.NewValue));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty AllowSubMeterProperty =
            DependencyProperty.Register(nameof(AllowSubMeter), typeof(bool), typeof(UtilityOptionControl),
                new PropertyMetadata(false, (d, e) =>
                    ((UtilityOptionControl)d)._items[UtilityBilling.SubMetered].Visibility =
                        (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

        public bool AllowSubMeter
        {
            get => (bool)GetValue(AllowSubMeterProperty);
            set => SetValue(AllowSubMeterProperty, value);
        }

        public UtilityOptionControl()
        {
            InitializeComponent();

            foreach (var (value, text) in Modes)
            {
                var item = new ComboBoxItem { Content = text, Tag = value };
                _items[value] = item;
                cmbBilling.Items.Add(item);
            }
            _items[UtilityBilling.SubMetered].Visibility = Visibility.Collapsed;

            cmbBilling.SelectedIndex = 0;
            cmbBilling.SelectionChanged += (_, _) => Refresh();
            Refresh();
        }

        private UtilityBilling SelectedBilling =>
            (cmbBilling.SelectedItem as ComboBoxItem)?.Tag is UtilityBilling b
                ? b : UtilityBilling.TenantPaysProvider;

        private bool IsIncluded => chkIncluded.IsChecked == true;

        private void Included_Changed(object sender, RoutedEventArgs e) => Refresh();

        private void Refresh()
        {
            detailsPanel.Visibility = IsIncluded ? Visibility.Collapsed : Visibility.Visible;

            var mode = SelectedBilling;
            amountPanel.Visibility = mode == UtilityBilling.TenantPaysProvider
                ? Visibility.Collapsed : Visibility.Visible;

            lblAmount.Text = mode switch
            {
                UtilityBilling.FixedMonthlyFee => "Monthly fee charged to tenant",
                UtilityBilling.SplitAmongTenants => "Total monthly cost to split",
                UtilityBilling.PerPersonRate => "Rate per person / month",
                UtilityBilling.SubMetered => "Rate per kWh",
                _ => "Amount"
            };
        }

        private bool TryParseAmount(out decimal amount) =>
            decimal.TryParse(txtAmount.Text.Replace("₱", "").Replace(",", "").Trim(),
                NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

        public UtilityOption Option
        {
            get
            {
                if (IsIncluded)
                    return new UtilityOption { Included = true };

                TryParseAmount(out decimal amount);
                var mode = SelectedBilling;
                return new UtilityOption
                {
                    Included = false,
                    Billing = mode,
                    Amount = mode == UtilityBilling.TenantPaysProvider ? 0 : amount,
                    Note = txtNote.Text.Trim()
                };
            }
            set
            {
                value ??= new UtilityOption();
                chkIncluded.IsChecked = value.Included;

                cmbBilling.SelectedItem = _items[value.Billing];
                if (cmbBilling.SelectedItem == null) cmbBilling.SelectedIndex = 0;

                txtAmount.Text = value.Amount > 0
                    ? value.Amount.ToString("0.##", CultureInfo.InvariantCulture) : "";
                txtNote.Text = value.Note ?? "";
                Refresh();
            }
        }

        public bool TryValidate(string utilityName, out string error)
        {
            error = "";
            if (IsIncluded || SelectedBilling == UtilityBilling.TenantPaysProvider)
                return true;

            if (!TryParseAmount(out decimal amount) || amount < 0)
            {
                error = $"Please enter a valid amount for {utilityName} billing (or choose \"Tenant pays provider directly\").";
                return false;
            }
            return true;
        }
    }
}