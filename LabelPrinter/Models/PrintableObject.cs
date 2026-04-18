using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LabelPrinter.Models
{
    public abstract class PrintableObject : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private double _width;
        private double _height;
        private double _rotation;

        // Position on the label (top-left) in device-independent units (1/96th inch)
        public double X { get => _x; set => SetProperty(ref _x, value); }
        public double Y { get => _y; set => SetProperty(ref _y, value); }

        // Size in device-independent units
        public double Width { get => _width; set => SetProperty(ref _width, value); }
        public double Height { get => _height; set => SetProperty(ref _height, value); }

        // Rotation in degrees clockwise
        public double Rotation { get => _rotation; set => SetProperty(ref _rotation, value); }

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
    }
}
