using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LabelPrinter.Models
{
    public abstract class PrintableObject : INotifyPropertyChanged, IDataErrorInfo
    {
        private double _xMm;
        private double _yMm;
        private double _widthMm;
        private double _heightMm;
        private double _rotationDegrees;

        public double XMm { get => _xMm; set => SetProperty(ref _xMm, value); }
        public double YMm { get => _yMm; set => SetProperty(ref _yMm, value); }
        public double WidthMm { get => _widthMm; set => SetProperty(ref _widthMm, value); }
        public double HeightMm { get => _heightMm; set => SetProperty(ref _heightMm, value); }
        public double RotationDegrees { get => _rotationDegrees; set => SetProperty(ref _rotationDegrees, value); }

        public abstract PrintableObject Clone();
        public string Error => string.Empty;
        public string this[string columnName] => GetValidationError(columnName);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void CopyLayoutTo(PrintableObject target)
        {
            target.XMm = XMm;
            target.YMm = YMm;
            target.WidthMm = WidthMm;
            target.HeightMm = HeightMm;
            target.RotationDegrees = RotationDegrees;
        }

        protected virtual string GetValidationError(string propertyName)
        {
            return propertyName switch
            {
                nameof(XMm) when !IsFinite(XMm) || XMm < 0 => "X must be 0 mm or greater.",
                nameof(YMm) when !IsFinite(YMm) || YMm < 0 => "Y must be 0 mm or greater.",
                nameof(WidthMm) when !IsPositiveFinite(WidthMm) => "Width must be greater than 0 mm.",
                nameof(HeightMm) when !IsPositiveFinite(HeightMm) => "Height must be greater than 0 mm.",
                nameof(RotationDegrees) when !IsFinite(RotationDegrees) => "Rotation must be a valid number.",
                _ => string.Empty
            };
        }

        protected static bool IsPositiveFinite(double value) => IsFinite(value) && value > 0.0;
        protected static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
