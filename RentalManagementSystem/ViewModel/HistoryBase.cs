using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RentalManagementSystem.ViewModels
{
    public abstract class HistoryBase<T> : INotifyPropertyChanged where T : class
    {
        private ObservableCollection<T> _items;
        private ObservableCollection<T> _filteredItems = new();
        private string _searchQuery = string.Empty;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private bool _isLoading;

        protected HistoryBase()
        {
            _items = new ObservableCollection<T>();
            _items.CollectionChanged += OnItemsCollectionChanged;
        }

        /// <summary>
        /// Master list of all items fetched from the database.
        /// Adding/removing items automatically re-applies the filter.
        /// </summary>
        public ObservableCollection<T> Items
        {
            get => _items;
            set
            {
                value ??= new ObservableCollection<T>();
                if (ReferenceEquals(_items, value)) return;

                _items.CollectionChanged -= OnItemsCollectionChanged;
                _items = value;
                _items.CollectionChanged += OnItemsCollectionChanged;

                OnPropertyChanged();
                ApplyFilter();
            }
        }

        /// <summary>
        /// Filtered collection bound to the DataGrid. The instance is replaced on every filter pass,
        /// so bind to it with {Binding ...FilteredItems} instead of assigning ItemsSource once in code-behind.
        /// </summary>
        public ObservableCollection<T> FilteredItems
        {
            get => _filteredItems;
            protected set
            {
                _filteredItems = value ?? new ObservableCollection<T>();
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(HasData));
            }
        }

        /// <summary>Text search (Receipt #, Invoice #, Reference, ...).</summary>
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                value ??= string.Empty;
                if (_searchQuery == value) return;
                _searchQuery = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        /// <summary>Start of the date range filter (inclusive).</summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate == value) return;
                _startDate = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        /// <summary>End of the date range filter (inclusive).</summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate == value) return;
                _endDate = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        /// <summary>Controls loading indicators / spinners.</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Total visible items after applying active filters.</summary>
        public int TotalCount => FilteredItems?.Count ?? 0;

        /// <summary>Toggles empty-state views when no records match the filters.</summary>
        public bool HasData => TotalCount > 0;

        // ---------- To implement in concrete view models ----------
        public abstract Task LoadDataAsync();
        public abstract void ApplyFilter();

        /// <summary>Clears all filters and re-applies the filter only once.</summary>
        public virtual void ResetFilters()
        {
            _searchQuery = string.Empty;
            _startDate = null;
            _endDate = null;
            OnPropertyChanged(nameof(SearchQuery));
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            ApplyFilter();
        }

        /// <summary>Helper for derived classes: publish a new filtered result set.</summary>
        protected void UpdateFilteredItems(IEnumerable<T> results)
        {
            FilteredItems = new ObservableCollection<T>(results);
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
