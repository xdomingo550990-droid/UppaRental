using RentalManagementSystem.DAO;
using RentalManagementSystem.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace RentalManagementSystem.ViewModels
{
    public class PaymentHistoryViewModel : HistoryBase<Payment>
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Id of the logged-in tenant. Must be set before LoadDataAsync().</summary>
        public int UserId { get; set; }

        /// <summary>Sum of the payments currently visible (after filters).</summary>
        public decimal TotalPaid => FilteredItems.Sum(p => p.Amount);

        /// <summary>Text for the summary bar, e.g. "Total paid: ₱37,500.00  •  3 payment(s)".</summary>
        public string HeaderSummaryText =>
            $"Total paid: ₱{TotalPaid.ToString("N2", Inv)}  •  {TotalCount} payment(s)";

        public override async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                int userId = UserId;   // copy so the background thread uses a stable value

                List<Payment> payments = userId > 0
                    ? await Task.Run(() => PaymentDao.GetByUser(userId))
                    : new List<Payment>();

                // Assign once so the filter runs a single time (not once per row).
                Items = new ObservableCollection<Payment>(payments);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public override void ApplyFilter()
        {
            IEnumerable<Payment> results = Items;

            string query = SearchQuery?.Trim() ?? string.Empty;
            if (query.Length > 0)
                results = results.Where(p => Matches(p, query));

            if (StartDate.HasValue)
            {
                DateTime start = StartDate.Value.Date;
                results = results.Where(p => p.PaymentDate.Date >= start);
            }

            if (EndDate.HasValue)
            {
                DateTime end = EndDate.Value.Date;
                results = results.Where(p => p.PaymentDate.Date <= end);
            }

            // Newest first. OrderBy is stable, so a payment inserted at index 0 stays on top for its date.
            UpdateFilteredItems(results.OrderByDescending(p => p.PaymentDate.Date));

            OnPropertyChanged(nameof(TotalPaid));
            OnPropertyChanged(nameof(HeaderSummaryText));
        }

        private static bool Matches(Payment p, string q) =>
               Has(p.ReceiptNumber, q)
            || Has(p.InvoiceNumber, q)
            || Has(p.ReferenceNumber, q)
            || Has(p.Period, q)
            || Has(p.PaymentMethod, q);

        private static bool Has(string? value, string q) =>
            value?.Contains(q, StringComparison.OrdinalIgnoreCase) == true;
    }
}
