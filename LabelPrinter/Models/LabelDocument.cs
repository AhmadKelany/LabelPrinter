using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LabelPrinter.Models
{
    public class LabelDocument : INotifyPropertyChanged, IDataErrorInfo
    {
        private double _widthMm = 50.0;
        private double _heightMm = 30.0;

        public LabelDocument()
        {
            Items.CollectionChanged += Items_CollectionChanged;
        }

        public double WidthMm { get => _widthMm; set => SetProperty(ref _widthMm, value); }
        public double HeightMm { get => _heightMm; set => SetProperty(ref _heightMm, value); }
        public ObservableCollection<PrintableObject> Items { get; } = new();
        public string Error => string.Empty;
        public string this[string columnName] => columnName switch
        {
            nameof(WidthMm) when !IsPositiveFinite(WidthMm) => "Label width must be greater than 0 mm.",
            nameof(HeightMm) when !IsPositiveFinite(HeightMm) => "Label height must be greater than 0 mm.",
            _ => string.Empty
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        public LabelDocument CreateSnapshot()
        {
            var snapshot = new LabelDocument
            {
                WidthMm = WidthMm,
                HeightMm = HeightMm
            };

            foreach (var item in Items)
            {
                snapshot.Items.Add(item.Clone());
            }

            return snapshot;
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Items));
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static bool IsPositiveFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
    }
}
